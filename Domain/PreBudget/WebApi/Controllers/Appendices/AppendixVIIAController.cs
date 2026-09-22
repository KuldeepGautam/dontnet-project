namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VII-A: Statement of recoveries taken in reduction of expenditure under a Major Head.</summary>
[ApiController]
[Route("api/appendix-viia")]
[Authorize]
public class AppendixVIIAController : ControllerBase
{
    private const string AppendixCode = "VII-A";

    private readonly AppendixDataRepository<AppendixRecoveries> _repository;
    private readonly AppendixPermissionService _permissionService;
    private readonly PreBudgetDbContext _db;

    public AppendixVIIAController(AppendixDataRepository<AppendixRecoveries> repository, AppendixPermissionService permissionService, PreBudgetDbContext db)
    {
        _repository = repository;
        _permissionService = permissionService;
        _db = db;
    }

    /// <summary>
    /// Major Head dropdown, scoped to the selected Demand (client review 2026-08-05): "select
    /// majorheadname from M_DemandMajorHead mh inner join M_MajorHead mj on mh.MajorHeadCode =
    /// mj.MajorHeadCode where mh.DemandId = @demandid". Corrected 2026-08-24 (client testing
    /// feedback, "load as per demand no, order by major head id in numeric form"): matches by the
    /// stable DemandNo first, same fallback pattern AppendixDataRepository.GetByDemandAndYearAsync
    /// uses everywhere else - M_DemandMajorHead rows can be entered under a different year's
    /// DemandId for the same business demand, which a strict DemandId-only match would miss. Also
    /// sorts by MajorHeadId (numeric) instead of MajorHeadName (alphabetical text).
    /// </summary>
    [HttpGet("major-heads")]
    public async Task<IActionResult> GetMajorHeads([FromQuery] int demandId, CancellationToken ct)
    {
        var demandNo = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => (int?)d.DemandNo)
            .FirstOrDefaultAsync(ct);

        var majorHeads = await (
            from dmh in _db.DemandMajorHeads
            join mh in _db.MajorHeads on dmh.MajorHeadCode equals mh.MajorHeadCode
            where ((demandNo != null && dmh.DemandNo == demandNo) || (dmh.DemandNo == null && dmh.DemandId == demandId))
                && dmh.IsActive && mh.IsActive
            select new { mh.MajorHeadId, mh.MajorHeadCode, mh.MajorHeadName }
        ).Distinct().ToListAsync(ct);

        return Ok(majorHeads
            .OrderBy(m => MajorHeadDisplayFormatter.NumericSortKey(m.MajorHeadCode))
            .Select(m => new
            {
                m.MajorHeadId,
                MajorHeadName = MajorHeadDisplayFormatter.Format(m.MajorHeadCode, m.MajorHeadName)
            }));
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records.ToList(), ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixRecoveriesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanCreate) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to add records to Appendix VII-A." });

        // "verify unique entry per Major Head and Scheme Name. A Major Head can have multiple
        // entries for different schemes" (client testing feedback, 2026-08-24) - the uniqueness
        // key is the Major Head + Scheme Name pair, not Major Head alone.
        if (await HasDuplicateAsync(request, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Major Head and Scheme Name." });
        }

        if (!IsAlphabeticSchemeName(request.SchemeName))
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Scheme Name must contain letters and spaces only." });
        }

        var entity = new AppendixRecoveries { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.SchemeName,
            request.MajorHeadId,
            request.IsCharged,
            request.Actuals,
            request.BE,
            request.RE,
            request.NBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixRecoveriesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanEdit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to edit Appendix VII-A records." });

        if (await HasDuplicateAsync(request, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Major Head and Scheme Name." });
        }

        if (!IsAlphabeticSchemeName(request.SchemeName))
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Scheme Name must contain letters and spaces only." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.SchemeName,
                request.MajorHeadId,
                request.IsCharged,
                request.Actuals,
                request.BE,
                request.RE,
                request.NBE), ct);
            return Ok(await ToDtoAsync(updated, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "UPDATE_FAILED", Message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanDelete) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to delete Appendix VII-A records." });

        try
        {
            await _repository.DeleteAsync(id, userId.Value, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "DELETE_FAILED", Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/freeze")]
    public async Task<IActionResult> FreezeRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanSubmit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to freeze Appendix VII-A records." });

        try
        {
            var frozen = await _repository.FreezeAsync(id, userId.Value, ct);
            return Ok(await ToDtoAsync(frozen, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    // "scheme name should not contain non-character (non english alphabet) characters" (client
    // testing feedback, 2026-08-24) - letters and spaces only, mirrored client-side via the view's
    // pattern attribute; enforced here too since the client-side check alone can be bypassed.
    private static bool IsAlphabeticSchemeName(string? schemeName) =>
        !string.IsNullOrWhiteSpace(schemeName) && Regex.IsMatch(schemeName, "^[A-Za-z ]+$");

    private async Task<bool> HasDuplicateAsync(SaveAppendixRecoveriesDto request, int? excludeId, CancellationToken ct)
    {
        var schemeName = (request.SchemeName ?? string.Empty).Trim();
        return await _db.AppendixVIIARecoveries.AsNoTracking().AnyAsync(e =>
            !e.IsDeleted
            && e.DemandId == request.DemandId
            && e.FinancialYear == request.FinancialYear
            && e.MajorHeadId == request.MajorHeadId
            && e.SchemeName != null && e.SchemeName.Trim().ToLower() == schemeName.ToLower()
            && (excludeId == null || e.Id != excludeId.Value), ct);
    }

    private async Task<AppendixRecoveriesDto> ToDtoAsync(AppendixRecoveries e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixRecoveries> { e }, ct);
        return dtos[0];
    }

    /// <summary>Batches the Major Head name lookup into 1 query - the grid shows the name, not the raw id (client review 2026-08-05).</summary>
    private async Task<List<AppendixRecoveriesDto>> ToDtosAsync(List<AppendixRecoveries> entities, CancellationToken ct)
    {
        var majorHeadIds = entities.Where(e => e.MajorHeadId.HasValue).Select(e => e.MajorHeadId!.Value).Distinct().ToList();
        var majorHeads = await _db.MajorHeads
            .Where(m => majorHeadIds.Contains(m.MajorHeadId))
            .ToDictionaryAsync(m => m.MajorHeadId, m => m, ct);

        return entities.Select(e => new AppendixRecoveriesDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            SchemeName = e.SchemeName,
            MajorHeadId = e.MajorHeadId,
            MajorHeadName = e.MajorHeadId.HasValue && majorHeads.TryGetValue(e.MajorHeadId.Value, out var mh)
                ? MajorHeadDisplayFormatter.Format(mh.MajorHeadCode, mh.MajorHeadName)
                : null,
            IsCharged = e.IsCharged,
            Actuals = e.Actuals,
            BE = e.BE,
            RE = e.RE,
            NBE = e.NBE,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
