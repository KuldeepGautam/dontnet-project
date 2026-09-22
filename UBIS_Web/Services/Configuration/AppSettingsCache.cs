namespace UBIS.Web.Services.Configuration;

using UBIS.Web.Services.Clients;

/// <summary>
/// Caches AIM's api/app-settings response (dbo.AppSettings' EnableEmail/EnableIPLogging/
/// EnableHttps/EnableCertificate flags) for CacheDuration, since UBIS_Web has no direct DB access
/// of its own (see AimClient.GetAppSettingsAsync) - avoids an HTTP round trip to AIM on every
/// request for ComplianceInterceptorFilter's per-request EnableIPLogging re-check. Falls back to
/// the last known-good value, or an all-off default if none is cached yet, if AIM is unreachable -
/// matches AppSettingsReader's same fail-safe behavior on the DB-backed services (a transient AIM
/// outage shouldn't flip a security-relevant flag's effective value). Added 2026-08-07.
/// </summary>
public class AppSettingsCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private readonly IAimClient _aimClient;
    private readonly ILogger<AppSettingsCache> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AppSettingsSnapshotDto? _cached;
    private DateTime _expiresAtUtc;

    public AppSettingsCache(IAimClient aimClient, ILogger<AppSettingsCache> logger)
    {
        _aimClient = aimClient;
        _logger = logger;
    }

    /// <summary>
    /// The <paramref name="ct"/> parameter is accepted for call-site symmetry with every other
    /// client method, but deliberately NOT threaded into the actual refill below (2026-08-20 fix).
    /// This is a process-wide singleton cache — the semaphore wait and the AIM call happen once
    /// per CacheDuration on behalf of every concurrent caller, not just whichever request happened
    /// to trigger the refill. Tying that shared work to one caller's RequestAborted token meant a
    /// user manually refreshing a page (browser cancels the in-flight request) could throw an
    /// OperationCanceledException out of _lock.WaitAsync mid-refill, which is confusing to see in
    /// logs (surfaces as an obscure "Thread was aborted"-style failure at that exact line) and,
    /// worse, could abort the refill out from under any other concurrent request still waiting on
    /// the same semaphore for legitimately fresh data. ComplianceInterceptorFilter still passes its
    /// own RequestAborted here for symmetry, but a cancelled caller now just stops awaiting its own
    /// call (ASP.NET Core drops the response either way); it no longer cancels the shared refill.
    /// </summary>
    public async Task<AppSettingsSnapshotDto> GetAsync(CancellationToken ct = default)
    {
        if (_cached != null && _expiresAtUtc > DateTime.UtcNow)
        {
            return _cached;
        }

        await _lock.WaitAsync(CancellationToken.None);
        try
        {
            if (_cached != null && _expiresAtUtc > DateTime.UtcNow)
            {
                return _cached;
            }

            var result = await _aimClient.GetAppSettingsAsync(CancellationToken.None);
            if (result.IsSuccess && result.Data != null)
            {
                _cached = result.Data;
                _expiresAtUtc = DateTime.UtcNow.Add(CacheDuration);
                return _cached;
            }

            _logger.LogWarning("Could not fetch app settings from AIM ({Error}); using {Source}.",
                result.Error?.Message, _cached != null ? "last known value" : "all-flags-off default");
            return _cached ?? new AppSettingsSnapshotDto();
        }
        finally
        {
            _lock.Release();
        }
    }
}
