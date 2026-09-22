namespace UBIS.Services.Ecl.Infrastructure.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

/// <summary>
/// Server-side DOE-role gate for EclApprovalController's approve/reject/reapproval-grant actions.
/// This is deliberately enforced here, not just hidden behind UI gating in UBIS_Web — a
/// Demand-role user holding a valid JWT could otherwise call this API directly and bypass any
/// UI-only restriction. Returns 403 if the caller's "RoleId" claim isn't in DoeRoleOptions.DoeRoleIds.
/// Applied as [ServiceFilter(typeof(RequireDoeRoleFilter))] so it isn't copy-pasted per action.
/// </summary>
public class RequireDoeRoleFilter : IAsyncActionFilter
{
    private readonly DoeRoleOptions _options;

    public RequireDoeRoleFilter(IOptions<DoeRoleOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.User.IsDoe(_options.DoeRoleIds))
        {
            context.Result = new ObjectResult(new { Code = "FORBIDDEN", Message = "This action requires a DOE role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
