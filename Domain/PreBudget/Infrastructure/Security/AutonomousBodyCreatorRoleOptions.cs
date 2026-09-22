namespace UBIS.Services.PreBudget.Infrastructure.Security;

/// <summary>
/// FR-004 role allow-list for directly adding a new Autonomous Body (client requirement,
/// 2026-08-31: "it should not request Autonomous Body, it should add it" - the Single Demand
/// Users role, previously routed through the pending-request/approval flow on the Autonomous
/// Master screen, now saves directly instead). Deliberately separate from
/// AutonomousBodyAdminRoleOptions, which gates reviewing/approving requests - Single Demand Users
/// may create directly but must not gain the reviewer/approval powers that options class still
/// gates. Binds to "PreBudget:AutonomousBodyCreatorRoles" in appsettings.json.
/// </summary>
public class AutonomousBodyCreatorRoleOptions
{
    public List<string> AllowedRoleNames { get; set; } = new()
    {
        "Administrator",
        "Super Admin",
        "Single Demand Users"
    };
}
