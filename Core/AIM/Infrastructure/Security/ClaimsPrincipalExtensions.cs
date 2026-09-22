namespace UBIS.Services.Aim.Infrastructure.Security;

using System.Security.Claims;

/// <summary>
/// Reads the JWT's <c>RoleId</c> claim (already issued by <c>AuthenticationService.GenerateJwtToken</c>)
/// to decide whether the caller holds one of the configured admin roles. Added 2026-07-13,
/// replacing the earlier invented <c>AIM:UserAdmin:R</c> function-based claim check.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsAdmin(this ClaimsPrincipal user, IReadOnlyCollection<int> adminRoleIds)
    {
        var roleIdClaim = user.FindFirst("RoleId")?.Value;
        return int.TryParse(roleIdClaim, out var roleId) && adminRoleIds.Contains(roleId);
    }
}
