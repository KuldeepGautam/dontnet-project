namespace UBIS.Services.Sbe.Infrastructure.Security;

using System.Security.Claims;

/// <summary>Reads the JWT's "RoleId"/"sub" claims (issued by AIM's AuthenticationService.GenerateJwtToken). Mirrors AIM's/ECL's own ClaimsPrincipalExtensions exactly.</summary>
public static class ClaimsPrincipalExtensions
{
    public static int? GetRoleId(this ClaimsPrincipal user)
    {
        var roleIdClaim = user.FindFirst("RoleId")?.Value;
        return int.TryParse(roleIdClaim, out var roleId) ? roleId : null;
    }

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>True if the caller's RoleId is in the given SBE role category, or holds the Administrator break-glass override.</summary>
    public static bool IsInSbeRole(this ClaimsPrincipal user, SbeRoleCategory category, SbeRoleOptions options)
    {
        var roleId = user.GetRoleId();
        if (roleId is null)
        {
            return false;
        }

        if (options.AdminRoleIds.Contains(roleId.Value))
        {
            return true;
        }

        var allowedRoleIds = category switch
        {
            SbeRoleCategory.Ministry => options.MinistryRoleIds,
            SbeRoleCategory.BudgetDivision => options.BudgetDivisionRoleIds,
            SbeRoleCategory.AboDsDirector => options.AboDsDirectorRoleIds,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        return allowedRoleIds.Contains(roleId.Value);
    }
}
