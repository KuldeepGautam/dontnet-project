namespace UBIS.Services.UserProfile.Application.Interfaces;

/// <summary>Calls AIM's existing profile endpoints server-to-server, forwarding the caller's own bearer token.</summary>
public interface IAimProfileClient
{
    Task<AimProfileSnapshot?> GetProfileSnapshotAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>Direct Email/Mobile update — AIM stays the sole M_User writer, this just forwards. Returns false on failure.</summary>
    Task<bool> UpdateContactAsync(string bearerToken, string? email, string? mobile, CancellationToken ct = default);
}

/// <summary>Merged result of AIM's GET /api/users/legacy-profile + GET /api/users/compliance-status.
/// MaskedMobile renamed from plaintext Mobile 2026-08-17 (AIM Workstream 7).</summary>
public sealed record AimProfileSnapshot(
    string? UserName,
    string? RoleName,
    DateTime? LastLoginDate,
    string? AllowedIpAddressOne,
    string? AllowedIpAddressTwo,
    string? Email,
    string? MaskedMobile);
