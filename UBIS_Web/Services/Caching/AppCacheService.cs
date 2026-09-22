namespace UBIS.Web.Services.Caching;

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Two-tier cache: an in-process <see cref="IMemoryCache"/> (L1 — microsecond reads, private
/// to this app-pool worker) in front of Redis via <see cref="IDistributedCache"/> (L2 — source
/// of truth, shared across every instance/server). L1 entries are capped to a short local TTL
/// regardless of the caller's requested expiration, bounding cross-instance staleness to a few
/// seconds — the right trade-off for read-heavy, tolerant-of-slight-staleness data like the
/// per-role menu tree (many concurrent users sharing a role would otherwise all force a fresh
/// MenuGenerator round-trip).
/// </summary>
public class AppCacheService : IAppCacheService
{
    private static readonly TimeSpan LocalTtl = TimeSpan.FromSeconds(5);

    private readonly IDistributedCache _distributed;
    private readonly IMemoryCache _local;
    private readonly ILogger<AppCacheService> _logger;

    public AppCacheService(IDistributedCache distributed, IMemoryCache local, ILogger<AppCacheService> logger)
    {
        _distributed = distributed;
        _local = local;
        _logger = logger;
    }

    /// <summary>
    /// Redis being briefly unreachable/slow, or the request being cancelled mid-call (e.g. a
    /// ViewComponent's <c>HttpContext.RequestAborted</c> firing), must never crash the caller —
    /// a cache is best-effort by definition. Any failure here degrades to a cache miss.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_local.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        try
        {
            var bytes = await _distributed.GetAsync(key, ct).ConfigureAwait(false);
            if (bytes == null)
            {
                return default;
            }

            var value = JsonSerializer.Deserialize<T>(bytes);
            _local.Set(key, value, LocalTtl);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppCacheService.GetAsync failed for key {Key}; falling back to cache miss.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await _distributed.SetAsync(
                key,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
                ct).ConfigureAwait(false);

            _local.Set(key, value, expiration < LocalTtl ? expiration : LocalTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppCacheService.SetAsync failed for key {Key}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _distributed.RemoveAsync(key, ct).ConfigureAwait(false);
            _local.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppCacheService.RemoveAsync failed for key {Key}.", key);
        }
    }
}
