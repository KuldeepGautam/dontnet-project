namespace UBIS.Web.Controllers.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

/// <summary>
/// Section 3 remote kill-switch: the client polls this every few seconds; once AIM reports the
/// caller's IP/Mobile change request was just approved, the browser shows a 60-second warning
/// then forces logout. Added 2026-07-10.
/// </summary>
[ApiController]
[Route("api/session")]
[Authorize]
public class SessionStatusController(IAimClient aimClient) : ControllerBase
{
    [HttpGet("change-request-status")]
    public async Task<IActionResult> GetChangeRequestStatus(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        var result = await aimClient.GetChangeRequestStatusAsync(session.Token, ct);
        return Ok(new { justApproved = result.IsSuccess && (result.Data?.JustApproved ?? false) });
    }

    /// <summary>
    /// Detects a password or role change made directly in the DB (not through this app) - client
    /// requirement 2026-08-10. Compares a fresh live read of dbo.M_Users.LastPasswordChangeDate /
    /// dbo.M_MapUserRole.RoleId against the baseline captured once at login
    /// (UbisSessionData.BaselinePasswordChangedAtUtc/BaselineRoleId - see that class's doc comment
    /// for why those never update again after the first capture). Same polling + kill-switch-warning
    /// UX as GetChangeRequestStatus above, reusing the identical client-side machinery
    /// (session-monitor.js) rather than a second bespoke pattern.
    /// </summary>
    /// <summary>
    /// Session Management (Redis Cache) test case, 2026-08-10: refreshes LastActivityUtc on the
    /// caller's ubis:session:{sessionId} Redis record. Piggybacks session-monitor.js's existing 5s
    /// poll interval rather than adding a second one - see AIM's SessionMonitorController doc
    /// comment for why this can't be a true per-request hook.
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        var result = await aimClient.SendSessionHeartbeatAsync(session.Token, ct);

        // Client requirement 2026-09-03: "by any case, if token get expired or invalidated, user
        // session should end and user should redirect to login page." UBIS_Web's own ASP.NET Core
        // Session (checked above) is independent of AIM's ubis:session:{sid} revocation key
        // (JwtSessionRevocationEvents) - a session revoked elsewhere (idle-timeout logout-all on
        // another tab/device, an admin force-logout, a password change) stays "logged in" here
        // until something actually calls out to a microservice with the now-dead JWT. This
        // heartbeat already fires every 5 seconds regardless of which page the user is on, so it's
        // the natural place to surface that fast instead of waiting for the user to navigate
        // somewhere else that happens to hit a live AIM/PreBudget call. Previously this result was
        // discarded entirely (always returned 200) - the exact "Chrome Network tab shows 200 even
        // though AIM rejected it" pattern already traced once this session (TokenRefreshFilter's
        // own race-condition fix). 401 specifically (not 503/other failures, which just mean AIM
        // itself is briefly unreachable - not that THIS session is dead) is forwarded to the
        // browser so session-monitor.js's sendHeartbeat() can force a real logout within one poll
        // cycle instead of silently continuing to look "logged in."
        if (!result.IsSuccess && result.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return Unauthorized();
        }

        return Ok();
    }

    [HttpGet("security-stamp-status")]
    public async Task<IActionResult> GetSecurityStampStatus(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        if (session.BaselineRoleId is null || session.BaselinePasswordChangedAtUtc is null)
        {
            // Baseline not captured yet (deferred bootstrap calls haven't completed) - nothing to
            // compare against, so nothing to report. Next poll will pick it up once they have.
            return Ok(new { changed = false });
        }

        var result = await aimClient.GetComplianceStatusAsync(session.Token, session.FinancialYear, ct);
        if (!result.IsSuccess || result.Data == null)
        {
            return Ok(new { changed = false });
        }

        var changed = result.Data.RoleId != session.BaselineRoleId
            || result.Data.PasswordChangedAtUtc != session.BaselinePasswordChangedAtUtc;

        return Ok(new { changed });
    }

    /// <summary>
    /// Idle-timeout revamp (client requirement 2026-09-03): "after timer countdown to 0 ... all
    /// sessions for the user should be invalidated" - called by session-monitor.js's forceLogout()
    /// right before it submits the existing /User/Logout form, so an idle session timing out also
    /// revokes any other device/tab the same user is logged in on (AIM's logout-all-sessions,
    /// ComplianceController), not just this one. Best-effort: if it fails (network hiccup, AIM
    /// down), the subsequent /User/Logout POST still clears this browser's own session/cookie
    /// exactly as before - this only adds the "everywhere" reach on top.
    /// </summary>
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        await aimClient.LogoutAllSessionsAsync(session.Token, ct);
        return Ok();
    }
}
