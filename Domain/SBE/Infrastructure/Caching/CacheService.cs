namespace UBIS.Services.Sbe.Infrastructure.Caching;

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using UBIS.Services.Sbe.Application.Interfaces;

/// <summary>
/// Two-tier cache (in-process <see cref="IMemoryCache"/> L1 in front of Redis L2), copied from
/// AIM's own CacheService template (see ICacheService's doc comment). Used for
/// Category/SubCategory/Scheme/SubScheme/UmbrellaScheme/MajorHead lookups and the resolved Demand
/// ceiling, invalidate-on-write for anything the Masters screens themselves edit.
/// </summary>
public class CacheService : ICacheService
{
    private static readonly TimeSpan LocalTtl = TimeSpan.FromSeconds(5);

    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _local;

    public CacheService(IConnectionMultiplexer redis, IMemoryCache local)
    {
        _redis = redis;
        _local = local;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        bool sliding = false,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var envelope = new CacheEnvelope<T>
        {
            Value = value,
            SlideSeconds = sliding && expiration.HasValue ? expiration.Value.TotalSeconds : null
        };

        await db.StringSetAsync(key, JsonSerializer.Serialize(envelope), expiration).ConfigureAwait(false);

        var localTtl = expiration.HasValue && expiration.Value < LocalTtl ? expiration.Value : LocalTtl;
        _local.Set(key, value, localTtl);
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (_local.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(key).ConfigureAwait(false);
        if (!raw.HasValue)
        {
            return default;
        }

        var envelope = JsonSerializer.Deserialize<CacheEnvelope<T>>((string)raw!);
        if (envelope == null)
        {
            return default;
        }

        if (envelope.SlideSeconds.HasValue)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(envelope.SlideSeconds.Value)).ConfigureAwait(false);
        }

        _local.Set(key, envelope.Value, LocalTtl);
        return envelope.Value;
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key).ConfigureAwait(false);
        _local.Remove(key);
    }

    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (_local.TryGetValue(key, out _))
        {
            return true;
        }

        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key).ConfigureAwait(false);
    }

    public async Task<long> IncrementWithExpiryAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        // Deliberately bypasses L1: read-then-write, not a Redis-atomic INCR - acceptable for
        // this cache's usage (nothing rate-limit-critical yet in SBE), matching AIM's own
        // template's documented trade-off exactly.
        var raw = await db.StringGetAsync(key).ConfigureAwait(false);
        long newValue = 1;
        if (raw.HasValue)
        {
            var existing = JsonSerializer.Deserialize<CacheEnvelope<long>>((string)raw!);
            if (existing != null)
            {
                newValue = existing.Value + 1;
            }
        }

        await db.StringSetAsync(key, JsonSerializer.Serialize(new CacheEnvelope<long> { Value = newValue }), expiration)
            .ConfigureAwait(false);
        _local.Remove(key);

        return newValue;
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyTimeToLiveAsync(key).ConfigureAwait(false);
    }

    public async Task ClearAllAsync(
        CancellationToken cancellationToken = default)
    {
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            await server.FlushDatabaseAsync().ConfigureAwait(false);
        }

        if (_local is MemoryCache concrete)
        {
            concrete.Compact(1.0);
        }
    }

    public Task<List<string>> GetKeysByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        var db = _redis.GetDatabase();
        foreach (var endpoint in _redis.GetEndPoints())
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsReplica)
            {
                foreach (var key in server.Keys(database: db.Database, pattern: pattern))
                {
                    keys.Add(key.ToString());
                }
            }
        }

        return Task.FromResult(keys);
    }

    private sealed class CacheEnvelope<T>
    {
        public T? Value { get; set; }

        public double? SlideSeconds { get; set; }
    }
}
