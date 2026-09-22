namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.DTOs.User;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Domain.Entities;
using UBIS.Services.Aim.Domain.Entities.Legacy;
using UBIS.Services.Aim.Infrastructure.Persistence;
using UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Section 3 compliance workflow: self-service Mobile/IP change requests against the legacy
/// <c>dbo.UserIPrequest</c> table, admin approval, and the polling endpoint the remote
/// kill-switch banner in UBIS_Web checks. Added 2026-07-10. Rewritten 2026-07-13: <c>UserId</c>
/// is now the same int identity throughout — no more separate legacy-record bridge.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class ComplianceController(
    AimDbContext db,
    ICacheService cache,
    IComplianceNotificationService notifications,
    IOptions<AdminRoleOptions> adminRoleOptions,
    IMobileProtectionService mobileProtectionService) : ControllerBase
{
    private static readonly TimeSpan NotifiedMarkerTtl = TimeSpan.FromHours(24);

    [HttpPost("change-request")]
    public async Task<IActionResult> SubmitChangeRequest([FromBody] SubmitChangeRequestDto request, CancellationToken ct)
    {
        var callerId = GetCallerUserId();

        var entry = new UserIPRequest
        {
            UsersId = callerId,
            DemandId = request.DemandId,
            IPadres1 = request.NewIPAddressOne,
            IPadres2 = request.NewIPAddressTwo,
            Mobile = request.NewMobile,
            RequestDate = DateTime.UtcNow,
            ApproveFlag = ApproveFlagValues.Pending
        };

        db.UserIPRequests.Add(entry);
        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            callerId, "ChangeRequestSubmitted",
            $"Mobile/IP change request #{entry.RowId} submitted for user {callerId}.", ct);

        return Ok(new { rowId = entry.RowId, status = entry.ApproveFlag });
    }

    [HttpGet("change-request/status")]
    public async Task<IActionResult> GetChangeRequestStatus(CancellationToken ct)
    {
        var callerId = GetCallerUserId();

        var latest = await db.UserIPRequests.AsNoTracking()
            .Where(r => r.UsersId == callerId)
            .OrderByDescending(r => r.RequestDate)
            .FirstOrDefaultAsync(ct);

        if (latest == null)
        {
            return Ok(new ChangeRequestStatusDto());
        }

        var justApproved = false;
        if (latest.ApproveFlag == ApproveFlagValues.Approved)
        {
            var notifiedKey = $"ubis:ipreq:notified:{latest.RowId}";
            var alreadyNotified = await cache.ExistsAsync(notifiedKey, ct);
            if (!alreadyNotified)
            {
                justApproved = true;
                await cache.SetAsync(notifiedKey, true, NotifiedMarkerTtl, cancellationToken: ct);
            }
        }

        return Ok(new ChangeRequestStatusDto
        {
            RowId = latest.RowId,
            ApproveFlag = latest.ApproveFlag,
            RequestDate = latest.RequestDate,
            ApproveDate = latest.ApproveDate,
            JustApproved = justApproved
        });
    }

    /// <summary>Lists all pending Mobile/IP change requests for admin review. Added 2026-07.</summary>
    [HttpGet("admin/change-requests/pending")]
    public async Task<IActionResult> GetPendingChangeRequests(CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var pendingRequests = await db.UserIPRequests.AsNoTracking()
            .Where(r => r.ApproveFlag == ApproveFlagValues.Pending)
            .OrderBy(r => r.RequestDate)
            .ToListAsync(ct);

        var userIds = pendingRequests.Select(r => r.UsersId).Distinct().ToList();
        var usersById = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, ct);

        var items = pendingRequests.Select(r =>
        {
            usersById.TryGetValue(r.UsersId, out var user);

            return new PendingChangeRequestDto
            {
                RowId = r.RowId,
                UserName = user?.Username ?? $"(user #{r.UsersId})",
                FullName = user?.FullName,
                RequestedMobile = r.Mobile,
                RequestedIPAddressOne = r.IPadres1,
                RequestedIPAddressTwo = r.IPadres2,
                RequestDate = r.RequestDate
            };
        }).ToList();

        return Ok(new AdminChangeRequestListDto { Items = items });
    }

    /// <summary>Admin rejection. Added 2026-07.</summary>
    [HttpPost("admin/change-requests/{rowId:int}/reject")]
    public async Task<IActionResult> RejectChangeRequest(int rowId, CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var entry = await db.UserIPRequests.FirstOrDefaultAsync(r => r.RowId == rowId, ct);
        if (entry == null)
        {
            return NotFound();
        }

        entry.ApproveFlag = ApproveFlagValues.Rejected;
        entry.ApproveDate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            GetCallerUserId(), "ChangeRequestRejected",
            $"Mobile/IP change request #{entry.RowId} rejected for user {entry.UsersId}.", ct);

        return Ok(new { rowId = entry.RowId, status = entry.ApproveFlag });
    }

    /// <summary>Admin approval — triggers the 60-second client-side kill-switch warning on next poll.</summary>
    [HttpPost("admin/change-requests/{rowId:int}/approve")]
    public async Task<IActionResult> ApproveChangeRequest(int rowId, CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var entry = await db.UserIPRequests.FirstOrDefaultAsync(r => r.RowId == rowId, ct);
        if (entry == null)
        {
            return NotFound();
        }

        entry.ApproveFlag = ApproveFlagValues.Approved;
        entry.ApproveDate = DateTime.UtcNow;
        entry.ApproveUserId = GetCallerUserId();

        // Apply the approved hardware IP change into dbo.M_MapUserIPAddress (extracted from
        // M_User.IPadres1/2 2026-08-17 - a user's allowed IPs are a grant, not core identity).
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == entry.UsersId, ct);
        if (user != null)
        {
            if (!string.IsNullOrEmpty(entry.Mobile)) user.EncryptedMobile = mobileProtectionService.Protect(entry.Mobile);

            if (!string.IsNullOrEmpty(entry.IPadres1) || !string.IsNullOrEmpty(entry.IPadres2))
            {
                var ipMapping = await db.MapUserIPAddresses.FirstOrDefaultAsync(m => m.UserId == entry.UsersId, ct);
                if (ipMapping == null)
                {
                    ipMapping = new UBIS.Services.Aim.Domain.Entities.MapUserIPAddress
                    {
                        UserId = entry.UsersId,
                        UserIdCreatedBy = GetCallerUserId(),
                        CreatedOnDate = DateTime.UtcNow
                    };
                    db.MapUserIPAddresses.Add(ipMapping);
                }

                if (!string.IsNullOrEmpty(entry.IPadres1)) ipMapping.IPAddress1 = entry.IPadres1;
                if (!string.IsNullOrEmpty(entry.IPadres2)) ipMapping.IPAddress2 = entry.IPadres2;
                ipMapping.UserIdModifyBy = GetCallerUserId();
                ipMapping.ModifiedOnDate = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            GetCallerUserId(), "ChangeRequestApproved",
            $"Mobile/IP change request #{entry.RowId} approved for user {entry.UsersId}.", ct);

        return Ok(new { rowId = entry.RowId, status = entry.ApproveFlag });
    }

    /// <summary>Admin unlock for a persistently-locked account (IsLocked, added 2026-08-17 — set
    /// after 5 consecutive failed login attempts). Resets FailedLoginAttempts so the user isn't
    /// immediately re-locked on their next attempt.</summary>
    [HttpPost("admin/{userId:int}/unlock")]
    public async Task<IActionResult> UnlockUser(int userId, CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            return NotFound();
        }

        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            GetCallerUserId(), "UserUnlocked",
            $"User {userId} was unlocked by an administrator.", ct);

        return Ok(new { userId, isLocked = user.IsLocked });
    }

    /// <summary>
    /// FR-006 (Token Refresh &amp; Revocation), added 2026-08-21: admin-triggered force logout.
    /// Deletes every <c>ubis:session:{sid}</c> Redis key belonging to this user (found the same way
    /// SessionMonitorController's Active Session Monitor already lists them — scan+filter, no
    /// separate per-user index needed at this scale). Every JWT-validating service's
    /// OnTokenValidated handler (JwtSessionRevocationEvents) checks this same key on every request,
    /// so the user's existing token(s) stop working immediately, not just at natural expiry.
    /// </summary>
    [HttpPost("admin/{userId:int}/force-logout")]
    public async Task<IActionResult> ForceLogoutUser(int userId, CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var revokedCount = await RevokeAllSessionsForUserAsync(userId, ct);

        db.SecurityEvents.Add(new SecurityEvent
        {
            EventType = "ForceLogout",
            UserId = userId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Timestamp = DateTime.UtcNow,
            Detail = $"Administrator force-logged-out user {userId}, revoking {revokedCount} active session(s)."
        });
        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            GetCallerUserId(), "ForceLogout",
            $"User {userId} was force-logged-out by an administrator ({revokedCount} session(s) revoked).", ct);

        return Ok(new { userId, revokedSessionCount = revokedCount });
    }

    /// <summary>
    /// Self-service "logout everywhere" (client requirement 2026-09-03, idle-timeout revamp):
    /// the caller revokes ALL of their OWN active sessions - unlike <see cref="ForceLogoutUser"/>
    /// above, this needs no admin role, since a caller can only ever target themselves (userId is
    /// read from their own JWT, never a request parameter). Used by UBIS_Web's idle-timeout
    /// countdown once it reaches 0, so an idle browser session timing out also invalidates any
    /// other device/tab the same user is logged in on ("all sessions for the user should be
    /// invalidated"), not just the idle one. Every JWT-validating service's OnTokenValidated
    /// handler (JwtSessionRevocationEvents) checks the same Redis key on every subsequent request,
    /// so this takes effect immediately everywhere, not just on next natural expiry.
    /// </summary>
    [HttpPost("logout-all-sessions")]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
    {
        var callerId = GetCallerUserId();
        var revokedCount = await RevokeAllSessionsForUserAsync(callerId, ct);

        db.SecurityEvents.Add(new SecurityEvent
        {
            EventType = "IdleTimeoutLogoutAll",
            UserId = callerId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Timestamp = DateTime.UtcNow,
            Detail = $"Idle-timeout logout revoked {revokedCount} active session(s) for this user."
        });
        await db.SaveChangesAsync(ct);

        return Ok(new { revokedSessionCount = revokedCount });
    }

    /// <summary>Shared by <see cref="ForceLogoutUser"/> and, indirectly, every password-change path
    /// via AuthenticationService's own copy of this same scan+filter — kept as two independent
    /// implementations (Controller vs. Service layer) rather than a shared helper, consistent with
    /// this solution's per-layer duplication convention elsewhere.</summary>
    private async Task<int> RevokeAllSessionsForUserAsync(int userId, CancellationToken ct)
    {
        var keys = await cache.GetKeysByPatternAsync("ubis:session:*", ct);
        var revoked = 0;
        foreach (var key in keys)
        {
            var record = await cache.GetAsync<SessionRecordDto>(key, ct);
            if (record?.UserId == userId)
            {
                await cache.RemoveAsync(key, ct);
                revoked++;
            }
        }
        return revoked;
    }

    /// <summary>
    /// One-time admin action (Workstream 7, added 2026-08-17): encrypts every still-plaintext
    /// <c>M_User.Mobile</c> value into the new <c>EncryptedMobile</c> column via this app's own
    /// Data Protection key ring, then leaves the legacy plaintext column untouched (it isn't
    /// dropped until <c>m-user-workstream-7-drop-mobile-columns.sql</c> is run, separately, once
    /// this has been verified). Safe to call more than once — only rows where EncryptedMobile is
    /// still null and the legacy Mobile is populated are touched, so an interrupted/partial run can
    /// just be re-invoked. EF isn't asked to map the legacy Mobile column (User.EncryptedMobile
    /// replaced it), so this reads it directly via raw SQL instead.
    /// </summary>
    [HttpPost("admin/backfill-encrypted-mobile")]
    public async Task<IActionResult> BackfillEncryptedMobile(CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var rows = await db.Database
            .SqlQuery<LegacyMobileRow>(
                $"SELECT UserId, Mobile AS PlainMobile FROM dbo.M_User WHERE Mobile IS NOT NULL AND LTRIM(RTRIM(Mobile)) <> '' AND EncryptedMobile IS NULL")
            .ToListAsync(ct);

        var migrated = 0;
        foreach (var row in rows)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == row.UserId, ct);
            if (user == null || !string.IsNullOrEmpty(user.EncryptedMobile))
            {
                continue;
            }

            user.EncryptedMobile = mobileProtectionService.Protect(row.PlainMobile);
            migrated++;
        }

        await db.SaveChangesAsync(ct);

        await notifications.NotifyProfileOrIpChangeAsync(
            GetCallerUserId(), "MobileBackfillCompleted",
            $"Encrypted-mobile backfill migrated {migrated} row(s).", ct);

        return Ok(new { migrated });
    }

    private sealed class LegacyMobileRow
    {
        public int UserId { get; set; }
        public string PlainMobile { get; set; } = string.Empty;
    }

    private int GetCallerUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst("UserId")?.Value;
        return int.TryParse(sub, out var id) ? id : 0;
    }
}
