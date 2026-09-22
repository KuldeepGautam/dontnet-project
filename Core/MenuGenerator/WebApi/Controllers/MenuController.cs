namespace UBIS.Services.MenuGenerator.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.MenuGenerator.Application.Interfaces;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ISessionRoleResolver _sessionRoleResolver;

    public MenuController(IMenuService menuService, ISessionRoleResolver sessionRoleResolver)
    {
        _menuService = menuService;
        _sessionRoleResolver = sessionRoleResolver;
    }

    /// <summary>
    /// Returns the App -> Module -> Function menu tree visible to the caller's role. The role is
    /// never taken from the caller — it's resolved from the "sid" claim on the caller's AIM-issued
    /// JWT, looked up against the same Redis session AIM opened at login.
    /// </summary>
    [HttpGet("full")]
    public async Task<IActionResult> GetFull(CancellationToken ct)
    {
        var sessionId = User.FindFirst("sid")?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized(new { error = "Token has no session id." });
        }

        var roleName = await _sessionRoleResolver.GetRoleNameAsync(sessionId, ct);
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Unauthorized(new { error = "Session expired or not found. Log in again." });
        }

        var result = await _menuService.GetFullMenuAsync(roleName, ct);
        if (result == null)
        {
            return NotFound(new { error = "Role not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Same menu tree, addressed directly by role name instead of a caller's session — for
    /// trusted internal tooling only (e.g. MenuScaffolder, which walks every role at design time
    /// and has no logged-in user/JWT to present). Security here is CallerRestrictionMiddleware's
    /// shared-secret header, the same gate every other MenuGenerator request already passes
    /// through — not JWT/session-based, since there is no session for a tool run.
    /// </summary>
    [HttpGet("by-role")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByRole([FromQuery] string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest(new { error = "roleName is required" });
        }

        var result = await _menuService.GetFullMenuAsync(roleName, ct);
        if (result == null)
        {
            return NotFound(new { error = "Role not found" });
        }

        return Ok(result);
    }
}
