namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Infrastructure.Configuration;
using UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Exposes dbo.AppSettings' well-known bit flags (EnableEmail/EnableIPLogging/EnableHttps/
/// EnableCertificate/EnableSms) plus dbo.AppSettingsInt's IdleTimerMinutes over HTTP for
/// UBIS_Web, which has no direct DB connection of its own (it talks to every backend service
/// over HTTP only - see UBIS_Web\Program.cs). The GET is AllowAnonymous because it's read before
/// UBIS_Web has any user session (needed at its own Program.cs startup for EnableHttps) -
/// protected instead by CallerRestrictionMiddleware, which already gates every endpoint in this
/// service to approved internal callers. The write endpoint below is the one exception - real
/// user-facing admin action, so it needs a real [Authorize] + IsAdmin check.
/// </summary>
[ApiController]
[Route("api/app-settings")]
public class AppSettingsController(AppSettingsReader reader, IOptions<AdminRoleOptions> adminRoleOptions) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var snapshot = await reader.GetAllAsync(ct);
        return Ok(snapshot);
    }

    /// <summary>
    /// Client requirement 2026-09-04: keeps AdminController.SessionSettings' existing admin page
    /// functional against the new DB-backed setting (dbo.AppSettingsInt.IdleTimer) instead of the
    /// old UBIS_Web-local Redis-cache override it used to write - see AppSettingsReader.SetIntAsync's
    /// own doc comment for why that mechanism was replaced.
    /// </summary>
    [Authorize]
    [HttpPut("idle-timer-minutes")]
    public async Task<IActionResult> UpdateIdleTimerMinutes([FromBody] UpdateIdleTimerMinutesRequest request, CancellationToken ct)
    {
        if (!User.IsAdmin(adminRoleOptions.Value.AdminRoleIds))
        {
            return Forbid();
        }

        if (request.Minutes is < 1 or > 120)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Enter a value between 1 and 120 minutes." });
        }

        await reader.SetIntAsync("IdleTimer", request.Minutes, ct);
        return Ok(new { minutes = request.Minutes });
    }
}

public class UpdateIdleTimerMinutesRequest
{
    public int Minutes { get; set; }
}
