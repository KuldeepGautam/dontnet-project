namespace UBIS.Services.Ecl.Infrastructure.Security;

using System.Security.Claims;

/// <summary>Reads the JWT's "RoleId" claim (issued by AIM's AuthenticationService.GenerateJwtToken) to decide whether the caller holds one of the configured DOE roles. Mirrors AIM's ClaimsPrincipalExtensions.IsAdmin exactly.</summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsDoe(this ClaimsPrincipal user, IReadOnlyCollection<int> doeRoleIds)
    {
        var roleIdClaim = user.FindFirst("RoleId")?.Value;
        return int.TryParse(roleIdClaim, out var roleId) && doeRoleIds.Contains(roleId);
    }

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }
}
