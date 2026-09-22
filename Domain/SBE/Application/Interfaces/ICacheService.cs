namespace UBIS.Services.Sbe.Application.Interfaces;

/// <summary>
/// Service interface for caching operations. Copied from AIM's own ICacheService (each
/// microservice keeps its own copy of this shape, not a shared package — matches JwtOptions/
/// AppSettingsReader/CallerRestrictionMiddleware's existing duplication convention). Backs
/// Category/SubCategory/Scheme/SubScheme/UmbrellaScheme/MajorHead lookups and the resolved Demand
/// ceiling — all read-heavy, low-change-frequency data the Masters/Add-SBE screens will need.
/// </summary>
public interface ICacheService
{
    /// <summary>Sets a value in cache with optional expiration.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        bool sliding = false,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a value from cache.</summary>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a value from cache.</summary>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Checks if key exists in cache.</summary>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Increments integer value in cache with expiration.</summary>
    Task<long> IncrementWithExpiryAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>Gets remaining TTL for a key.</summary>
    Task<TimeSpan?> GetTimeToLiveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Clears all cache entries (use with caution in production).</summary>
    Task ClearAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Lists every key matching a Redis glob pattern.</summary>
    Task<List<string>> GetKeysByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default);
}
