namespace UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Service interface for caching operations.
/// Provides methods to interact with Redis cache for session management and brute force protection.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    /// <typeparam name="T">Type of value to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Optional expiration duration.</param>
    /// <param name="sliding">If true, expiration slides on each access.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        bool sliding = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache.
    /// </summary>
    /// <typeparam name="T">Type of value to retrieve.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached value or default if not found.</returns>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if key exists in cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if key exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments integer value in cache with expiration.
    /// Used for rate limiting and failed login attempt tracking.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="expiration">Expiration duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Incremented value.</returns>
    Task<long> IncrementWithExpiryAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets remaining TTL for a key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>TimeSpan with remaining TTL, or null if key doesn't exist.</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries (use with caution in production).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every key matching a Redis glob pattern (e.g. "ubis:session:*") - backs the Active
    /// Session Monitor admin page (added 2026-08-10). Talks to Redis directly (SCAN via
    /// IServer.Keys), bypassing the L1 memory cache entirely, since L1 has no concept of "all keys."
    /// </summary>
    Task<List<string>> GetKeysByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default);
}
