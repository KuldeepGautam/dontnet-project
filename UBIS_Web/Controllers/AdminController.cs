namespace UBIS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UBIS.Web.Configuration;
using UBIS.Web.Models.Admin;
using UBIS.Web.Services.Caching;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Configuration;
using UBIS.Web.Services.Logging;
using UBIS.Web.Services.Session;
using UBIS.Web.ViewComponents;

/// <summary>
/// Admin-only screens (added 2026-07): reviewing pending Mobile/IP change requests and setting
/// the session countdown at runtime. Gated on the caller's Role (added 2026-07-13, replacing the
/// earlier invented <c>AIM:UserAdmin:R</c> claim — no real Function backed it) — mirrors the same
/// RoleId check AIM's own <c>ComplianceController</c> uses for its approve endpoint, so "who can
/// approve" stays consistent between the two services.
/// </summary>
[Authorize]
public class AdminController : Controller
{
    private readonly IAimClient _aimClient;
    private readonly IAppCacheService _cache;
    private readonly AppSettingsCache _appSettingsCache;
    private readonly IWebLogClient _logClient;
    private readonly AdminRoleOptions _adminRoleOptions;

    public AdminController(
        IAimClient aimClient, IAppCacheService cache, AppSettingsCache appSettingsCache, IWebLogClient logClient,
        IOptions<AdminRoleOptions> adminRoleOptions)
    {
        _aimClient = aimClient;
        _cache = cache;
        _appSettingsCache = appSettingsCache;
        _logClient = logClient;
        _adminRoleOptions = adminRoleOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> PendingRequests(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var result = await _aimClient.GetPendingChangeRequestsAsync(session.Token, ct);
        return View(new PendingRequestsViewModel
        {
            Items = result.Data?.Items ?? new List<PendingChangeRequestDto>(),
            StatusMessage = TempData["Admin.StatusMessage"] as string,
            StatusIsError = TempData["Admin.StatusIsError"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int rowId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var result = await _aimClient.ApproveChangeRequestAsync(session.Token, rowId, ct);
        await _logClient.InfoAsync("Admin approved change request", new { session.UserName, rowId, result.IsSuccess }, ct);

        TempData["Admin.StatusMessage"] = result.IsSuccess
            ? $"Request #{rowId} approved."
            : result.Error?.Message ?? $"Could not approve request #{rowId}.";
        TempData["Admin.StatusIsError"] = !result.IsSuccess;

        return RedirectToAction(nameof(PendingRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int rowId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var result = await _aimClient.RejectChangeRequestAsync(session.Token, rowId, ct);
        await _logClient.InfoAsync("Admin rejected change request", new { session.UserName, rowId, result.IsSuccess }, ct);

        TempData["Admin.StatusMessage"] = result.IsSuccess
            ? $"Request #{rowId} rejected."
            : result.Error?.Message ?? $"Could not reject request #{rowId}.";
        TempData["Admin.StatusIsError"] = !result.IsSuccess;

        return RedirectToAction(nameof(PendingRequests));
    }

    [HttpGet]
    public async Task<IActionResult> SessionSettings(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var settings = await _appSettingsCache.GetAsync(ct);

        return View(new SessionSettingsViewModel
        {
            CurrentCountdownMinutes = settings.IdleTimerMinutes,
            StatusMessage = TempData["Admin.StatusMessage"] as string,
            StatusIsError = TempData["Admin.StatusIsError"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SessionSettings(SessionSettingsViewModel model, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        if (model.CurrentCountdownMinutes is < 1 or > 120)
        {
            model.StatusMessage = "Enter a value between 1 and 120 minutes.";
            model.StatusIsError = true;
            return View(model);
        }

        // Client requirement 2026-09-04: writes dbo.AppSettingsInt.IdleTimer (a real, visible DB
        // row) via AIM instead of the old UBIS_Web-local Redis-cache override - see
        // SessionCountdownViewComponent's own doc comment for why that mechanism was replaced.
        // AppSettingsCache (read side, ≤60s TTL) may briefly still show the old value to any
        // request served in the next few seconds, same as every other AppSettings flag already
        // accepts - not treated as an error here.
        var updateResult = await _aimClient.UpdateIdleTimerMinutesAsync(session.Token, model.CurrentCountdownMinutes, ct);
        if (!updateResult.IsSuccess)
        {
            model.StatusMessage = updateResult.Error?.Message ?? "Could not update the session countdown.";
            model.StatusIsError = true;
            return View(model);
        }

        await _logClient.InfoAsync("Admin changed session countdown", new { session.UserName, model.CurrentCountdownMinutes }, ct);

        TempData["Admin.StatusMessage"] = "Session countdown updated.";
        TempData["Admin.StatusIsError"] = false;
        return RedirectToAction(nameof(SessionSettings));
    }

    /// <summary>Active Session Monitor - Session Management (Redis Cache) test case, 2026-08-10.</summary>
    [HttpGet]
    public async Task<IActionResult> ActiveSessions(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var result = await _aimClient.GetActiveSessionsAsync(session.Token, ct);
        return View(new ActiveSessionsViewModel
        {
            Sessions = result.Data ?? new List<ActiveSessionDto>(),
            StatusMessage = result.IsSuccess ? null : (result.Error?.Message ?? "Could not load active sessions."),
            StatusIsError = !result.IsSuccess
        });
    }

    /// <summary>FR-006 (Token Refresh &amp; Revocation), added 2026-08-21: force-logs-out every
    /// session a given user currently holds, from the Active Session Monitor page.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceLogout(int userId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return RedirectToAction("Login", "User");
        }

        if (!session.IsAdmin(_adminRoleOptions.AdminRoleIds))
        {
            return Forbidden();
        }

        var result = await _aimClient.ForceLogoutUserAsync(session.Token, userId, ct);
        await _logClient.InfoAsync("Admin force-logged-out user", new { session.UserName, userId, result.IsSuccess }, ct);

        TempData["Admin.StatusMessage"] = result.IsSuccess
            ? $"User #{userId} was force-logged-out."
            : result.Error?.Message ?? $"Could not force-logout user #{userId}.";
        TempData["Admin.StatusIsError"] = !result.IsSuccess;

        return RedirectToAction(nameof(ActiveSessions));
    }

    private ContentResult Forbidden() =>
        new() { StatusCode = StatusCodes.Status403Forbidden, Content = "You do not have permission to access this page.", ContentType = "text/plain" };
}
