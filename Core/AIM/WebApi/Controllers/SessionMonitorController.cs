namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.DTOs.User;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Session Management (Redis Cache) test case, 2026-08-10: backs the Active Session Monitor admin
/// page and the "Last Activity timestamp is updated after each authenticated request" acceptance
/// criterion. The heartbeat below is called periodically from UBIS_Web (piggybacking
/// session-monitor.js's existing 5s poll) rather than hooked into literal every request across all
/// 6 JWT-validating services - see AuthenticationService.IssueLoginResultAsync's neighboring code
/// for why per-request stateless JWT validation can't cheaply do this (no OnTokenValidated event
/// exists anywhere in this solution). Practically equivalent for an active browser tab; a session
/// with no open tab simply stops refreshing LastActivityUtc and ages out via the existing 5-minute
/// sliding Redis TTL, which is itself already "session activity" in the sense that mattered before
/// this feature existed.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class SessionMonitorController(ICacheService cache, IOptions<AdminRoleOptions> adminRoleOptions) : ControllerBase
{
    [HttpPost("authentication/session-heartbeat")]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        var sessionId = User.FindFirst("sid")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }

        var key = $"ubis:session:{sessionId}";
        var record = await cache.GetAsync<SessionRecordDto>(key, ct);
        if (record == null)
        {
            // Session already expired/logged out - nothing to refresh. Not an error: the caller
            // (session-monitor.js) just stops mattering once the browser's own auth cookie/session
            // also expires or Logout runs.
            return Ok(new { found = false });
        }

        record.LastActivityUtc = DateTime.UtcNow;
        await cache.SetAsync(key, record, TimeSpan.FromMinutes(5), sliding: true, cancellationToken: ct);

        return Ok(new { found = true });
    }

    /// <summary>
    /// Active Session Monitor - admin-only, lists every live ubis:session:* record. Routed under
    /// api/users/admin/... (not api/admin/...) to match ComplianceController's existing admin
    /// endpoints - the Gateway's ocelot.json only has a catch-all route for api/users/{everything}
    /// and api/authentication/{everything}, nothing for a bare api/admin/* prefix.
    /// </summary>
    [HttpGet("users/admin/active-sessions")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        var keys = await cache.GetKeysByPatternAsync("ubis:session:*", ct);
        var sessions = new List<ActiveSessionDto>();

        foreach (var key in keys)
        {
            var record = await cache.GetAsync<SessionRecordDto>(key, ct);
            if (record == null)
            {
                continue;
            }

            var ttl = await cache.GetTimeToLiveAsync(key, ct);
            sessions.Add(new ActiveSessionDto
            {
                SessionId = key.Replace("ubis:session:", string.Empty),
                UserId = record.UserId,
                UserName = record.UserName,
                RoleName = record.RoleName,
                LoginTimeUtc = record.LoginTimeUtc,
                IPAddress = record.IPAddress,
                Status = record.Status,
                LastActivityUtc = record.LastActivityUtc,
                TimeToLiveSeconds = ttl?.TotalSeconds
            });
        }

        return Ok(sessions.OrderByDescending(s => s.LastActivityUtc).ToList());
    }
}
