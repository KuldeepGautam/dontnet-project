namespace UBIS.Services.MenuGenerator.Infrastructure.Caching;

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using UBIS.Services.MenuGenerator.Application.Interfaces;

/// <summary>
/// Two-tier cache: an in-process <see cref="IMemoryCache"/> (L1 — microsecond reads, private
/// to this app-pool worker) in front of Redis (L2 — source of truth, shared across every
/// instance/server). L1 entries are capped to <see cref="LocalTtl"/> regardless of the
/// caller's requested expiration, bounding cross-instance staleness to a few seconds while
/// absorbing the overwhelming majority of reads under concurrent load (e.g. many users sharing
/// a role all hitting the same cached menu tree).
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan LocalTtl = TimeSpan.FromSeconds(5);

    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _local;

    public RedisCacheService(IConnectionMultiplexer redis, IMemoryCache local)
    {
        _redis = redis;
        _local = local;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_local.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key).ConfigureAwait(false);
        if (!value.HasValue)
        {
            return default;
        }

        var deserialized = JsonSerializer.Deserialize<T>((string)value!);
        _local.Set(key, deserialized, LocalTtl);
        return deserialized;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiration).ConfigureAwait(false);

        _local.Set(key, value, expiration < LocalTtl ? expiration : LocalTtl);
    }
}
