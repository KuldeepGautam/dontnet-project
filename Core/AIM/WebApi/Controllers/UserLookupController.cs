namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Persistence;
using UBIS.Services.Aim.Infrastructure.Security;

[ApiController]
[Route("api/users")]
public class UserLookupController : ControllerBase
{
    private readonly AimDbContext _db;
    private readonly IUserRoleService _userRoleService;
    private readonly IStatementAccessService _statementAccessService;
    private readonly AdminRoleOptions _adminRoleOptions;
    private readonly IMobileProtectionService _mobileProtectionService;

    public UserLookupController(
        AimDbContext db,
        IUserRoleService userRoleService,
        IStatementAccessService statementAccessService,
        IOptions<AdminRoleOptions> adminRoleOptions,
        IMobileProtectionService mobileProtectionService)
    {
        _db = db;
        _userRoleService = userRoleService;
        _statementAccessService = statementAccessService;
        _adminRoleOptions = adminRoleOptions.Value;
        _mobileProtectionService = mobileProtectionService;
    }

    [HttpGet("role")]
    [Authorize]
    public async Task<IActionResult> GetRole([FromQuery] int? userId, [FromQuery] string? financialYear)
    {
        var callerId = GetCallerUserId();
        var targetUserId = userId ?? callerId;

        if (userId.HasValue && userId.Value != callerId && !User.IsAdmin(_adminRoleOptions.AdminRoleIds))
            return Forbid();

        var req = new UBIS.Services.Aim.Application.DTOs.User.GetUserRoleRequestDto { FinancialYear = financialYear ?? string.Empty };
        var res = await _userRoleService.GetUserRoleAsync(targetUserId, req);
        if (!res.IsSuccess) return Problem(detail: res.Error?.Message, title: res.Error?.Code);
        return Ok(res.Data);
    }

    [HttpGet("email")]
    [Authorize]
    public async Task<IActionResult> GetEmail([FromQuery] int? userId)
    {
        var callerId = GetCallerUserId();
        var targetUserId = userId ?? callerId;

        var isSelf = targetUserId == callerId;
        var hasAdmin = User.IsAdmin(_adminRoleOptions.AdminRoleIds);

        if (!isSelf && !hasAdmin) return Forbid();

        var user = await _db.Users.FindAsync(targetUserId);
        if (user == null) return NotFound();

        if (!isSelf && hasAdmin)
        {
            _db.SecurityEvents.Add(new UBIS.Services.Aim.Domain.Entities.SecurityEvent
            {
                EventType = "EmailLookupByAdmin",
                UserId = callerId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Detail = $"Lookup target:{targetUserId}"
            });
            await _db.SaveChangesAsync();
            return Ok(new { userId = user.UserId, email = MaskEmail(user.Email ?? string.Empty) });
        }

        return Ok(new { userId = user.UserId, email = user.Email });
    }

    /// <summary>
    /// Self-only compliance snapshot backing UBIS_Web's IP-binding/freeze/staleness
    /// interceptors (Section 2/4 of the compliance brief). Added 2026-07-10; sourced directly
    /// from <c>dbo.M_Users</c> as of 2026-07-13 (no more separate legacy-record bridge).
    /// </summary>
    [HttpGet("compliance-status")]
    [Authorize]
    public async Task<IActionResult> GetComplianceStatus([FromQuery] string? financialYear)
    {
        var callerId = GetCallerUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == callerId);
        if (user == null)
        {
            return NotFound();
        }

        int? roleId = null;
        if (!string.IsNullOrEmpty(financialYear))
        {
            var roleResult = await _userRoleService.GetUserRoleAsync(callerId, new Application.DTOs.User.GetUserRoleRequestDto { FinancialYear = financialYear });
            if (roleResult.IsSuccess && roleResult.Data != null)
            {
                roleId = roleResult.Data.Role.RoleId;
            }
        }

        var ipMapping = await _db.MapUserIPAddresses.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == callerId);

        return Ok(new UBIS.Services.Aim.Application.DTOs.User.ComplianceStatusDto
        {
            IsAccountFrozen = !user.IsActive,
            AllowedIpAddressOne = ipMapping?.IPAddress1,
            AllowedIpAddressTwo = ipMapping?.IPAddress2,
            RequireIpValidation = string.Equals(ipMapping?.IPAuthFlag, "Y", StringComparison.OrdinalIgnoreCase),
            PasswordChangedAtUtc = user.LastPasswordChangeDate,
            EmailLastValidatedAtUtc = user.EmailLastValidatedAtUtc,
            RoleId = roleId
        });
    }

    /// <summary>
    /// Section 3 Profile Dashboard data — sourced directly from <c>dbo.M_Users</c> (no more
    /// separate legacy-record bridge as of 2026-07-13), Role via <c>dbo.M_MapUserRole</c>
    /// (reintroduced 2026-07-13, user-confirmed; renamed 2026-08-04 from M_MapUserDemandFY).
    /// Added 2026-07-10.
    /// </summary>
    [HttpGet("legacy-profile")]
    [Authorize]
    public async Task<IActionResult> GetLegacyProfile()
    {
        var callerId = GetCallerUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == callerId);
        if (user == null)
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.LegacyProfileDto { HasLegacyRecord = false });
        }

        var userCharge = await _db.MapUserRoles.AsNoTracking()
            .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive);
        var roleName = userCharge != null
            ? await _db.Roles.AsNoTracking().Where(r => r.RoleId == userCharge.RoleId).Select(r => r.RoleName).FirstOrDefaultAsync()
            : null;

        var departmentId = await _db.MapUserDepartments.AsNoTracking()
            .Where(d => d.UserId == user.UserId)
            .Select(d => (int?)d.DepartmentId)
            .FirstOrDefaultAsync();

        return Ok(new UBIS.Services.Aim.Application.DTOs.User.LegacyProfileDto
        {
            HasLegacyRecord = true,
            Username = user.Username,
            Role = roleName,
            DepartmentId = departmentId,
            Email = user.Email,
            MaskedMobile = MaskedMobileOrNull(user.EncryptedMobile),
            UserCreationDate = user.EntryDate,
            CreatedBy = user.UserIdCreatedBy,
            LastLoginDate = user.LastLoginDate
        });
    }

    /// <summary>
    /// "My Assigned Statements/Profiles" for the Profile page — Statements the caller owns
    /// (<c>M_StmtControl.StmtUserId</c>/<c>StmtUserId1</c>) plus non-owner assignments
    /// (<c>dbo.UserId_StmtId_Mapping</c>). Added 2026-07.
    /// </summary>
    [HttpGet("assigned-statements")]
    [Authorize]
    public async Task<IActionResult> GetAssignedStatements([FromQuery] string financialYear)
    {
        var callerId = GetCallerUserId();
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.AssignedStatementsDto());
        }

        var statements = await _statementAccessService.GetAssignedStatementsAsync(callerId, financialYear);

        return Ok(new UBIS.Services.Aim.Application.DTOs.User.AssignedStatementsDto
        {
            Statements = statements.Select(s => new UBIS.Services.Aim.Application.DTOs.User.AssignedStatementDto
            {
                StmtId = s.StmtId,
                StmtNo = s.StmtNo,
                StmtName = s.StmtName
            }).ToList()
        });
    }

    /// <summary>
    /// "My Demands" for the Pre-Budget Meeting module's Demand-selection dropdown (and any future
    /// consumer needing the same list) — reads the caller's own <c>AIM:Demand:{id}</c> claims
    /// (already issued at login, see AuthenticationService.GenerateJwtToken) rather than
    /// re-deriving from M_MapUserRole/M_MapUserDemand a second time, then resolves those
    /// IDs against dbo.M_Demand for the requested year to attach DemandNo/DemandName. Added
    /// 2026-07-23 — until now the accessible Demand IDs only ever existed as boolean claims, never
    /// as a queryable named list. This is the single query every module's Demand-selection screen
    /// calls (PreBudget's Appendix main screen, Allocation screen, ECL) — so the DemandType == "E"
    /// filter added 2026-08-28 (client requirement) applies everywhere from this one place.
    /// </summary>
    [HttpGet("demands")]
    [Authorize]
    public async Task<IActionResult> GetMyDemands([FromQuery] string financialYear)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.MyDemandsDto());
        }

        var demandIds = User.Claims
            .Where(c => c.Type.StartsWith("AIM:Demand:", StringComparison.Ordinal))
            .Select(c => c.Type["AIM:Demand:".Length..])
            .Select(idText => int.TryParse(idText, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (demandIds.Count == 0)
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.MyDemandsDto());
        }

        // No IsActive filter: confirmed live that dbo.M_Demand.IsActive is NULL for every real row
        // in this dataset (never populated) — filtering on it silently excluded every demand.
        var demands = await _db.Demands.AsNoTracking()
            .Where(d => demandIds.Contains(d.DemandId) && d.FinancialYear == financialYear && d.DemandType == "E")
            .OrderBy(d => d.DemandNo)
            .ToListAsync();

        return Ok(new UBIS.Services.Aim.Application.DTOs.User.MyDemandsDto
        {
            Demands = demands.Select(d => new UBIS.Services.Aim.Application.DTOs.User.DemandDto
            {
                DemandId = d.DemandId,
                DemandNo = d.DemandNo,
                DemandName = d.DemandName
            }).ToList()
        });
    }

    /// <summary>
    /// Self-service Email/Mobile update — saves immediately (unlike the IP/legacy-Mobile
    /// change-request flow at SubmitChangeRequestAsync/M_UserIPrequest, which needs admin
    /// approval). Resets EmailLastValidatedAtUtc when Email changes so the existing 180-day
    /// staleness nudge (Dashboard) doesn't immediately re-fire for a freshly-confirmed address.
    /// Added 2026-07 for the UserProfile microservice.
    /// </summary>
    [HttpPut("contact")]
    [Authorize]
    public async Task<IActionResult> UpdateContact([FromBody] UBIS.Services.Aim.Application.DTOs.User.UpdateContactRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Email is not a valid email address." });
        }

        if (!string.IsNullOrWhiteSpace(request.Mobile) && !System.Text.RegularExpressions.Regex.IsMatch(request.Mobile, @"^\d{10}$"))
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Mobile must be exactly 10 digits." });
        }

        var callerId = GetCallerUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == callerId);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = request.Email;
            user.EmailLastValidatedAtUtc = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(request.Mobile))
        {
            user.EncryptedMobile = _mobileProtectionService.Protect(request.Mobile);
        }

        await _db.SaveChangesAsync();

        return Ok(new { user.Email, MaskedMobile = MaskedMobileOrNull(user.EncryptedMobile) });
    }

    /// <summary>
    /// Resolves contact details for users in the given roles, for the Pre-Budget "Add Allocation"
    /// screen's Email/SMS-to-recipients feature — the caller (PreBudget's WebApi) never has direct
    /// DB access to dbo.M_MapUserRole/M_User, so it asks AIM. When <paramref name="demandId"/> is
    /// given, only users whose dbo.M_MapUserDemand CSV includes that Demand (or "ALL") qualify -
    /// matches Demand-scoped roles like Budget Officer/CCA/FA. When omitted, every active user in
    /// the given roles qualifies regardless of Demand - matches org-wide Budget/MOF roles like
    /// Budget Division Officer/Section User, which aren't Demand-scoped. Added 2026-08-14.
    /// </summary>
    [HttpGet("contacts-by-role")]
    [Authorize]
    public async Task<IActionResult> GetContactsByRole([FromQuery] string roleNames, [FromQuery] int? demandId)
    {
        var wantedRoleNames = roleNames
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (wantedRoleNames.Count == 0)
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.ContactsByRoleResultDto());
        }

        var roleRows = await _db.MapUserRoles
            .Where(m => m.IsActive)
            .Join(_db.Roles.Where(r => wantedRoleNames.Contains(r.RoleName)),
                m => m.RoleId, r => r.RoleId, (m, r) => new { m.UserId, r.RoleName })
            .ToListAsync();

        if (roleRows.Count == 0)
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.ContactsByRoleResultDto());
        }

        if (demandId.HasValue)
        {
            var candidateUserIds = roleRows.Select(r => r.UserId).Distinct().ToList();
            var demandCsvByUser = await _db.UserDemandMappings
                .Where(m => candidateUserIds.Contains(m.UserId) && m.IsActive != false)
                .ToListAsync();

            var allowedUserIds = new HashSet<int>();
            foreach (var mapping in demandCsvByUser)
            {
                if (string.IsNullOrWhiteSpace(mapping.DemandIds)) continue;

                foreach (var part in mapping.DemandIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.Equals(part, "ALL", StringComparison.OrdinalIgnoreCase) ||
                        (int.TryParse(part, out var mappedDemandId) && mappedDemandId == demandId.Value))
                    {
                        allowedUserIds.Add(mapping.UserId);
                        break;
                    }
                }
            }

            roleRows = roleRows.Where(r => allowedUserIds.Contains(r.UserId)).ToList();
        }

        if (roleRows.Count == 0)
        {
            return Ok(new UBIS.Services.Aim.Application.DTOs.User.ContactsByRoleResultDto());
        }

        var userIds = roleRows.Select(r => r.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var contacts = roleRows
            .Where(r => users.ContainsKey(r.UserId))
            .Select(r =>
            {
                var user = users[r.UserId];
                return new UBIS.Services.Aim.Application.DTOs.User.ContactByRoleDto
                {
                    UserId = user.UserId,
                    Name = user.FullName,
                    Email = user.Email,
                    // Real (decrypted) number — this DTO exists specifically so the caller can
                    // actually dispatch an SMS/email to these recipients (see its doc comment).
                    Mobile = _mobileProtectionService.Unprotect(user.EncryptedMobile),
                    RoleName = r.RoleName
                };
            })
            .ToList();

        return Ok(new UBIS.Services.Aim.Application.DTOs.User.ContactsByRoleResultDto { Contacts = contacts });
    }

    private int GetCallerUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst("UserId")?.Value;
        return int.TryParse(sub, out var id) ? id : 0;
    }

    private string? MaskedMobileOrNull(string? encryptedMobile)
    {
        var plainMobile = _mobileProtectionService.Unprotect(encryptedMobile);
        return string.IsNullOrEmpty(plainMobile) ? null : _mobileProtectionService.Mask(plainMobile);
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var local = parts[0];
        var domain = parts[1];
        if (local.Length <= 2) return "**@" + domain;
        return local.Substring(0, 2) + "***@" + domain;
    }
}
