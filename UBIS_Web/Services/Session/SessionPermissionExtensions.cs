namespace UBIS.Web.Services.Session;

/// <summary>
/// Reads AIM's existence-based Function access list — <see cref="UbisSessionData.Permissions"/>
/// holds the FunctionIds the caller's Role has access to (no CRUD granularity; the real
/// dbo.M_MapRoleFunction table has no such columns — see AIM's AuthenticationService for the
/// full rationale). Rewritten 2026-07-13, replacing the earlier invented
/// "FunctionCode:CRUDMatrix" string shape. Admin gating now uses <see cref="IsAdmin"/>
/// (session.RoleId against a configured admin-RoleId list) instead of a Function-based claim.
/// </summary>
public static class SessionPermissionExtensions
{
    public static bool HasPermission(this UbisSessionData session, int functionId) =>
        session.Permissions.Contains(functionId);

    public static bool IsAdmin(this UbisSessionData session, IReadOnlyCollection<int> adminRoleIds) =>
        adminRoleIds.Contains(session.RoleId);
}
