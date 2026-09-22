namespace UBIS.Services.Sbe.Infrastructure.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

/// <summary>
/// Server-side role gate for actions restricted to exactly one of SBE's three FRS roles (Ministry
/// User / Budget Division User / ABO-DS-Director User) — deliberately enforced here, not just
/// hidden behind UI gating in UBIS_Web (same "UI-hiding is not enough" rule ECL's
/// RequireDoeRoleFilter already established for this repo). Returns 403 if the caller's "RoleId"
/// claim isn't in the requested category (Administrator always passes, see SbeRoleOptions).
/// </summary>
public class RequireSbeRoleFilter : IAsyncActionFilter
{
    private readonly SbeRoleCategory _category;
    private readonly SbeRoleOptions _options;

    public RequireSbeRoleFilter(SbeRoleCategory category, IOptions<SbeRoleOptions> options)
    {
        _category = category;
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.User.IsInSbeRole(_category, _options))
        {
            context.Result = new ObjectResult(new { Code = "FORBIDDEN", Message = $"This action requires the {_category} role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

/// <summary>Usage: [RequireSbeRole(SbeRoleCategory.BudgetDivision)] on a controller/action.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSbeRoleAttribute : TypeFilterAttribute
{
    public RequireSbeRoleAttribute(SbeRoleCategory category) : base(typeof(RequireSbeRoleFilter))
    {
        Arguments = new object[] { category };
    }
}
