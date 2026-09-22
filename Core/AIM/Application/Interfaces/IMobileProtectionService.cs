namespace UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Encrypts/decrypts/masks the 10-digit mobile number stored in <c>User.EncryptedMobile</c>.
/// Added 2026-08-17 (Workstream 7: Mobile encryption + masking) — backed by ASP.NET Core's Data
/// Protection API (<see cref="Infrastructure.Security.MobileProtectionService"/>), not a
/// hand-rolled cipher. Never call <see cref="Protect"/>/<see cref="Unprotect"/> for anything other
/// than <c>User.EncryptedMobile</c> — this is not a general-purpose crypto helper.
/// </summary>
public interface IMobileProtectionService
{
    /// <summary>Encrypts a plaintext 10-digit mobile number for storage in <c>User.EncryptedMobile</c>.</summary>
    string Protect(string plainMobile);

    /// <summary>
    /// Decrypts a value previously produced by <see cref="Protect"/>. Returns null (never throws)
    /// if <paramref name="encryptedMobile"/> is null/empty or isn't valid ciphertext for this
    /// service's key ring (e.g. malformed/legacy data) — callers should treat that the same as
    /// "no mobile on file" rather than fail the request.
    /// </summary>
    string? Unprotect(string? encryptedMobile);

    /// <summary>
    /// Masks a plaintext mobile number for display: all but the last 4 characters become lowercase
    /// 'x' (e.g. "9876543210" -&gt; "xxxxxx3210"). Strings of 4 characters or fewer come back as
    /// all 'x' of the same length. Instance method (not static) so DI callers holding only the
    /// interface can call it directly — <see cref="Infrastructure.Security.MobileProtectionService"/>
    /// also exposes an equivalent public static overload for non-DI call sites.
    /// </summary>
    string Mask(string plainMobile);
}
