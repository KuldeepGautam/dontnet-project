namespace UBIS.Services.Ecl.Infrastructure.Security;

/// <summary>
/// Configures which dbo.M_Role.RoleId values are treated as "DOE" for gating
/// EclApprovalController's approve/reject/reapproval-grant actions. Mirrors AIM's AdminRoleOptions
/// shape/DI-binding pattern exactly. This is a server-side check — a Demand-role user with a valid
/// JWT must not be able to call these actions directly, bypassing any UI-only gating in UBIS_Web.
///
/// Corrected AGAIN 2026-08-27 per explicit client direction: "ECL NIC" (55) and "ECL Expenditure"
/// (54) should both be approver/rejecter for Data To Approve. This reverses the 2026-08-20
/// correction below that excluded ECL NIC — the client's own niceclexp test user (RoleId 54, "ECL
/// Expenditure") was getting "This action requires a DOE role" when trying to approve, which is
/// exactly what that 2026-08-20 change caused for both these roles. Section User (25) is left in
/// place since nothing said to remove it; see
/// ecl-workstream-14-restore-ecl-nic-approval-role.sql for the matching dbo.M_MapRoleFunction
/// grant restoration for RoleId 55 (the 2026-08-20 change had also revoked its menu access to
/// these same screens, not just the server-side gate).
///
/// Prior history (2026-08-20): RoleId 25 "Section User" was made the sole non-Admin approver,
/// reasoning ECL NIC (55, the 2026-08-18 original guess, justified at the time by live LoginIds
/// eclnic/nicecl/dirbudecl/budgetecl/emcuser matching the ECL-NIC designer-mockup persona) was
/// read-only: view ECL Report screens and export to PDF/Excel, not approve/reject. That reasoning
/// is superseded by this correction. Administrator (13) stays as a break-glass override, matching
/// this repo's existing AdminRoleOptions convention everywhere else.
/// </summary>
public class DoeRoleOptions
{
    public List<int> DoeRoleIds { get; set; } = new() { 54, 55, 25, 13 };
}
