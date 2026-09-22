namespace UBIS.Services.PreBudget.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UBIS.Services.PreBudget.Infrastructure.Security;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// One-time, admin-only trigger for LegacyAppendixDataMigrationService (migrates Appendix I, I-A,
/// II, V-A and V-C data out of BIMSDemo - Appendix III/III-A/IV/IV-A/IV-B/V-B already have their
/// own migrations). Reuses the same "Administrator"/"Super Admin" allow-list already enforced for
/// Autonomous Body review (FR-004) rather than introducing a new role config surface.
/// </summary>
[ApiController]
[Route("api/admin/legacy-appendix-migration")]
[Authorize]
public class LegacyAppendixMigrationController : ControllerBase
{
    private readonly LegacyAppendixDataMigrationService _migrationService;
    private readonly AutonomousBodyAdminRoleOptions _adminRoleOptions;

    public LegacyAppendixMigrationController(LegacyAppendixDataMigrationService migrationService, IOptions<AutonomousBodyAdminRoleOptions> adminRoleOptions)
    {
        _migrationService = migrationService;
        _adminRoleOptions = adminRoleOptions.Value;
    }

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll(CancellationToken ct)
    {
        var roleName = GetRoleNameFromClaims();
        if (roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no role claim." });
        }

        if (!_adminRoleOptions.AllowedRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var results = await _migrationService.MigrateAllAsync(ct);
        return Ok(results);
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
