namespace UBIS.Web.Services.Session;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UBIS.Web.Services.Configuration;

/// <summary>
/// Global Section-2 security interceptor, evaluated on every authenticated request:
/// 1. Default/forced password reset — blocks every route except the reset flow itself until resolved.
/// 2. Hardware IP binding — drops the session instantly if the request's remote IP no longer
///    matches either allowed address (re-checked every request, not just at login). Enabled only
///    while <c>dbo.AppSettings.EnableIPLogging</c> is set (added 2026-07-24 as
///    Security:EnableUserIPSettings, replacing an earlier IsDevelopmentTime-based gate — that flag
///    also controlled AIM's internal-caller header check, so flipping it for this purpose silently
///    changed that unrelated one too; moved to the DB-backed AppSettings table 2026-08-07, read via
///    AIM's api/app-settings endpoint since UBIS_Web has no direct DB access — now reads the exact
///    same row AIM's own AuthenticationService.IsIpBindingEnabledAsync does, so the two can no
///    longer drift the way the old hand-maintained pair of config values could). Without this,
///    every real M_User row's legacy office IP (IPadres1/IPadres2) never matches a dev/test
///    machine, so the first click after ANY login killed the session.
///    Also skipped per-user when <see cref="UbisSessionData.RequireIpValidation"/> is false (added
///    2026-07-23) — an admin-granted bypass via M_Users.IPAuthFlag != "Y".
/// 3. Account freeze (UserFreez/UserMFFreez on the bridged legacy record) — blocks mutating
///    (non-GET) requests while frozen; read access is still allowed.
/// Added 2026-07-10.
/// </summary>
public class ComplianceInterceptorFilter : IAsyncActionFilter
{
    private readonly AppSettingsCache _appSettingsCache;

    public ComplianceInterceptorFilter(AppSettingsCache appSettingsCache)
    {
        _appSettingsCache = appSettingsCache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        // CompleteForcedPasswordChange (2026-07-20) is the AJAX counterpart to ForcePasswordReset
        // itself — the Login page's change-password dialog calls it while PasswordResetRequired
        // is still true, so it needs the same exemption or this filter redirects it away before
        // it ever gets to actually clear that flag.
        var isExempt = string.Equals(controllerName, "User", StringComparison.OrdinalIgnoreCase)
            && actionName is "Logout" or "ForcePasswordReset" or "CompleteForcedPasswordChange" or "Error";

        var session = httpContext.Session.GetUbisSession();
        if (session == null)
        {
            await next();
            return;
        }

        // Client report 2026-08-27 ("on deleting existing record from grid, page or demand
        // dropdown or appendix dropdown refreshes, that closes the appendix and ask to select
        // appendix again... check it for all appendix actions of delete, modify or save") - this
        // filter runs on every authenticated request, including the fetch()-driven Save/Freeze/
        // Delete calls the Pre-Budget Data and Report page makes into #partialViewContainer. A raw
        // RedirectToActionResult here (below) was silently followed by fetch() straight through to
        // the full ForcePasswordReset/Login page's HTML, which then got stuffed into that small
        // container - the exact same "dropdowns look wiped" bug class already fixed for
        // ExpiredSessionRedirectAsync/FreezeAppendixTemplate (PreBudgetMeetingController.cs), just
        // never applied here since this filter predates/is independent of those fixes. For an AJAX
        // request, return 401 with the intended destination in the body instead, so the client JS
        // (redirectToLoginIfSessionExpired, pre-budget-meeting.js) can do a REAL, visible
        // navigation there.
        var isAjax = string.Equals(httpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        // 1. Default password enforcement.
        if (session.PasswordResetRequired && !isExempt)
        {
            context.Result = isAjax
                ? new UnauthorizedObjectResult(new { redirectUrl = "/User/ForcePasswordReset" })
                : new RedirectToActionResult("ForcePasswordReset", "User", routeValues: null);
            return;
        }

        // 2. Hardware IP binding, re-checked every request.
        var appSettings = await _appSettingsCache.GetAsync(httpContext.RequestAborted);
        var ipBindingEnabled = appSettings.EnableIPLogging;
        var hasIpConfigured = ipBindingEnabled
            && session.RequireIpValidation
            && (!string.IsNullOrEmpty(session.AllowedIpAddressOne) || !string.IsNullOrEmpty(session.AllowedIpAddressTwo));
        if (hasIpConfigured)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
            var ipMatches = remoteIp == session.AllowedIpAddressOne || remoteIp == session.AllowedIpAddressTwo;
            if (!ipMatches)
            {
                httpContext.Session.ClearUbisSession();
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = isAjax
                    ? new UnauthorizedObjectResult(new { redirectUrl = "/User/Login?hardwareMismatch=true" })
                    : new RedirectToActionResult("Login", "User", new { hardwareMismatch = true });
                return;
            }
        }

        // 3. Account freeze — read-only while frozen.
        if (session.IsAccountFrozen && !isExempt && !HttpMethods.IsGet(httpContext.Request.Method))
        {
            context.Result = new ObjectResult(new { error = "This account is frozen and cannot submit changes. Contact your administrator." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
