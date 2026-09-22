namespace UBIS.Services.PreBudget.Infrastructure.Security;

/// <summary>
/// FR-004 role allow-list for reviewing/approving Autonomous Body requests. Binds to
/// "PreBudget:AutonomousBodyAdminRoles" in appsettings.json. Confirmed 2026-07-24 against the
/// live dbo.M_Role table — same source used for RemarkRoleOptions.
/// </summary>
public class AutonomousBodyAdminRoleOptions
{
    public List<string> AllowedRoleNames { get; set; } = new()
    {
        "Administrator",
        "Super Admin"
    };
}
