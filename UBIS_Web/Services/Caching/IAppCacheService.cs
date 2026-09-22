namespace UBIS.Web.Services.Caching;

public interface IAppCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
