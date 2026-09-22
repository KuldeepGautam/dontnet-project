namespace UBIS.Web.ViewComponents;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Services.Configuration;

/// <summary>
/// Resolves the effective session-countdown minutes from dbo.AppSettingsInt.IdleTimer (via AIM's
/// api/app-settings, cached - see AppSettingsCache), the same DB-backed pattern EnableHttps/
/// EnableIPLogging etc. already use.
///
/// Replaced 2026-09-04 (client report: "Idle timer is still starting from 13:00 from 15:00...
/// What ever value of this field, the timer should start from same value"): the previous
/// mechanism stored an admin-set "override" in UBIS_Web's own IAppCacheService (Redis) with a
/// 10-year TTL - not a real, visible setting - so a stale value set once during testing sat there
/// indefinitely, silently overriding the documented 15-minute default with no trace of where it
/// came from. AdminController.SessionSettings's admin page still exists and still writes a value,
/// but now via AIM's PUT api/app-settings/idle-timer-minutes (a real DB row), not this old cache
/// key - <see cref="CacheKey"/>/that Redis entry are no longer read by anything.
/// </summary>
public class SessionCountdownViewComponent : ViewComponent
{
    [Obsolete("No longer read - dbo.AppSettingsInt.IdleTimer (via AppSettingsCache) is now the source of truth. Kept only so any stale Redis entry from before 2026-09-04 is harmless, unreferenced dead data rather than a compile error.")]
    public const string CacheKey = "settings:session-countdown-minutes";

    private readonly AppSettingsCache _appSettingsCache;

    public SessionCountdownViewComponent(AppSettingsCache appSettingsCache)
    {
        _appSettingsCache = appSettingsCache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await _appSettingsCache.GetAsync(HttpContext.RequestAborted);
        return View(settings.IdleTimerMinutes);
    }
}
