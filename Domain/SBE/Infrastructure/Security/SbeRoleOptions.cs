namespace UBIS.Services.Sbe.Infrastructure.Security;

/// <summary>
/// Configures which dbo.M_Role.RoleId values map to each of the FRS's three SBE roles (§3.3
/// role-access matrix: Ministry User, Budget Division User, ABO/DS/Director User), for gating
/// controller actions that must be restricted to exactly one of them. Mirrors ECL's
/// DoeRoleOptions shape/DI-binding pattern, but with three separate role categories instead of
/// one flat DOE list, since the FRS distinguishes which of the three roles can reach which
/// action (not just "any privileged role").
///
/// Confirmed via direct M_Role query (2026-08-25) against UBIS-Dev:
/// - Ministry User = RoleId 18 "Single Demand Users" — same equivalence PreBudget's own
///   AutonomousMasterforAppendixVICandVIE access-check comment already established for this
///   system's Ministry-User-shaped role.
/// - Budget Division User = RoleId 21 "Budget Division" (name matches the FRS role literally).
/// - ABO/DS/Director User = RoleId 23 "ABO-DS-Director" (name matches the FRS role literally).
///
/// **Not yet confirmed with the client**: two more SBE-specific roles already exist in M_Role —
/// RoleId 36 "Admin SBE" and RoleId 38 "Section SBE" — that don't map cleanly onto the FRS's
/// stated 3-role model. Flag to the client/BA before Stage 2 (Masters) starts: are these meant
/// to be additional Administrator-equivalent/section-level roles layered on top of the 3 FRS
/// roles, or should they be folded into one of the three lists below?
/// </summary>
public class SbeRoleOptions
{
    public List<int> MinistryRoleIds { get; set; } = new() { 18 };
    public List<int> BudgetDivisionRoleIds { get; set; } = new() { 21 };
    public List<int> AboDsDirectorRoleIds { get; set; } = new() { 23 };

    /// <summary>Break-glass override, matching this repo's existing AdminRoleOptions convention everywhere else — always passes any role-category check.</summary>
    public List<int> AdminRoleIds { get; set; } = new() { 13 };
}

public enum SbeRoleCategory
{
    Ministry,
    BudgetDivision,
    AboDsDirector
}
