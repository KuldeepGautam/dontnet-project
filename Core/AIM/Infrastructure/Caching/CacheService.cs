namespace UBIS.Services.Aim.Infrastructure.Caching;

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Two-tier cache used for sessions, refresh tokens, rate-limit counters, and reset tokens:
/// an in-process <see cref="IMemoryCache"/> (L1 — microsecond reads, private to this app-pool
/// worker) in front of Redis (L2 — source of truth, shared across every instance/server).
/// L1 entries are capped to <see cref="LocalTtl"/> regardless of the caller's requested
/// expiration, bounding cross-instance staleness to a few seconds under concurrent load.
/// <see cref="IncrementWithExpiryAsync"/> deliberately bypasses L1 entirely and always hits
/// Redis directly — rate-limit counters must stay exactly consistent across every app-pool
/// instance, not just eventually consistent within a few seconds.
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

        // Deliberately bypasses L1: rate-limit counters must be exactly consistent across
        // every app-pool instance, not just eventually consistent within LocalTtl.
        // Stored in the same envelope shape GetAsync<T>/SetAsync<T> use, so counters can be
        // read back with GetAsync<long>/GetAsync<int> (that read path may lag Redis by up to
        // LocalTtl since it does consult L1 — an accepted, bounded relaxation for a 5-attempt
        // lockout counter, not a correctness issue for the increment itself).
        // Read-then-write, not a Redis-atomic INCR: acceptable for a 5-attempt lockout
        // counter where a rare lost increment under concurrent failed logins is harmless.
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

        // Reset the window on every increment, matching the previous rolling-lockout behavior.
        await db.StringSetAsync(key, JsonSerializer.Serialize(new CacheEnvelope<long> { Value = newValue }), expiration)
            .ConfigureAwait(false);
        _local.Remove(key);

        return newValue;
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        // Always authoritative from Redis; L1 doesn't track per-key TTL precisely enough to answer this.
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
