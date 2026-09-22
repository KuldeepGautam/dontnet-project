namespace UBIS.Services.MenuGenerator.Infrastructure.Services;

using UBIS.Services.MenuGenerator.Application.Interfaces;

/// <summary>
/// Reads the RoleName straight from the same "ubis:session:{sessionId}" Redis record AIM's
/// AuthenticationService writes at login — MenuGenerator never trusts a client-supplied roleName,
/// only the JWT's "sid" claim (which points at this record).
/// </summary>
public class SessionRoleResolver : ISessionRoleResolver
{
    private readonly ICacheService _cache;

    public SessionRoleResolver(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<string?> GetRoleNameAsync(string sessionId, CancellationToken ct = default)
    {
        var envelope = await _cache.GetAsync<AimCacheEnvelope>($"ubis:session:{sessionId}", ct).ConfigureAwait(false);
        return envelope?.Value?.RoleName;
    }

    /// <summary>
    /// AIM's own CacheService (Core/AIM/Infrastructure/Caching/CacheService.cs) wraps every stored
    /// value in this envelope for sliding-expiry support — a plain flat deserialize of
    /// AimSessionData against the raw Redis string would silently come back empty, since the real
    /// JSON is {"Value": {...}, "SlideSeconds": ...}, not the inner object directly.
    /// </summary>
    private sealed record AimCacheEnvelope(AimSessionData? Value, double? SlideSeconds);

    /// <summary>Matches the JSON shape AIM's AuthenticationService.IssueLoginResultAsync writes as the envelope's Value.</summary>
    private sealed record AimSessionData(int UserId, string UserName, int RoleId, string RoleName, List<int> Permissions);
}
