namespace UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Configures which <c>dbo.M_Role.RoleId</c> values are treated as "admin" for gating endpoints
/// like <c>ComplianceController</c>'s approve/reject/pending-list actions. Added 2026-07-13,
/// replacing the earlier invented <c>AIM:UserAdmin:R</c> claim (no real Function backed it — see
/// COMPLIANCE_NOTES.md). Default of <c>[13]</c> is the real "Administrator" RoleId confirmed from
/// the DBA's <c>M_Role</c> export.
/// </summary>
public class AdminRoleOptions
{
    public List<int> AdminRoleIds { get; set; } = new() { 13 };
}
