namespace UBIS.Services.PreBudget.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix-level permission lookup for the caller's own role (VII-A/VII-B/XI/PA-ReceiptPayment framework).</summary>
[ApiController]
[Route("api/appendix-permissions")]
[Authorize]
public class AppendixPermissionsController : ControllerBase
{
    private readonly AppendixPermissionService _permissionService;

    public AppendixPermissionsController(AppendixPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions([FromQuery] string appendixCode, CancellationToken ct)
    {
        var roleName = User?.FindFirst("RoleName")?.Value;
        if (roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no role claim." });
        }

        var permissions = await _permissionService.GetPermissionsAsync(roleName, appendixCode, ct);
        return Ok(permissions);
    }
}
