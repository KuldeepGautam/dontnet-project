namespace UBIS.Web.Configuration;

/// <summary>
/// Binds to "AdminRoles". Mirrors AIM's own <c>SecurityConfiguration:AdminRoles:AdminRoleIds</c>
/// (added 2026-07-13, replacing the earlier invented <c>AIM:UserAdmin:R</c> claim) — keep the two
/// in sync, since UBIS_Web has no direct access to AIM's config.
/// </summary>
public class AdminRoleOptions
{
    public List<int> AdminRoleIds { get; set; } = new() { 13 };
}
