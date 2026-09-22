namespace UBIS.Services.PreBudget.Infrastructure.Security;

/// <summary>
/// FR-003 role allow-list for RE Data Remarks (§8.2/§8.3 of the design doc) — Ministry/Department
/// users must be blocked entirely. Binds to "PreBudget:RemarkRoles" in appsettings.json.
/// Configurable rather than hardcoded since these are real dbo.M_Role.RoleName seed values, not
/// FRS wording — confirmed 2026-07-24 against the live table ("Budget Division", "ABO-DS-Director").
/// </summary>
public class RemarkRoleOptions
{
    public List<string> AllowedRoleNames { get; set; } = new()
    {
        "Budget Division",
        "ABO-DS-Director",
        "Administrator"
    };
}
