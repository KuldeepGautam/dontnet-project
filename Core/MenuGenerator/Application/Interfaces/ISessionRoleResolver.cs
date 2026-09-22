namespace UBIS.Services.MenuGenerator.Application.Interfaces;

/// <summary>
/// Resolves the caller's RoleName from the Redis session AIM opened at login (looked up by the
/// "sid" claim on the caller's JWT) — the role never comes from a client-supplied value.
/// </summary>
public interface ISessionRoleResolver
{
    Task<string?> GetRoleNameAsync(string sessionId, CancellationToken ct = default);
}
