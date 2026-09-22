namespace UBIS.Services.Aim.Infrastructure.Security;

using Microsoft.AspNetCore.DataProtection;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// ASP.NET Core Data-Protection-backed implementation of <see cref="IMobileProtectionService"/>.
/// Added 2026-08-17 (Workstream 7). Uses a dedicated purpose string so this ciphertext can never be
/// swapped in for/with any other Data-Protection-protected value in the process, per the API's own
/// purpose-isolation guidance. The protector is resolved once per instance (IDataProtectionProvider
/// is registered singleton by AddDataProtection, so this is cheap regardless of this service's own
/// DI lifetime).
/// </summary>
public class MobileProtectionService : IMobileProtectionService
{
    private const string Purpose = "UBIS.AIM.Mobile.v1";

    private readonly IDataProtector _protector;

    public MobileProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plainMobile) => _protector.Protect(plainMobile);

    /// <summary>
    /// Decrypts, swallowing exactly the exception Data Protection throws for ciphertext it can't
    /// make sense of (wrong purpose, wrong/rotated-away key, or genuinely malformed/legacy data) —
    /// same defensive pattern as AuthenticationService.TryVerifyPassword: treat "can't recover this
    /// value" as a normal null result, never a 500.
    /// </summary>
    public string? Unprotect(string? encryptedMobile)
    {
        if (string.IsNullOrEmpty(encryptedMobile))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(encryptedMobile);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            // Payload isn't even valid base64 (e.g. stray legacy/garbage data) — Unprotect expects
            // a base64 string and throws FormatException before it ever gets to the crypto layer.
            return null;
        }
    }

    string IMobileProtectionService.Mask(string plainMobile) => Mask(plainMobile);

    /// <summary>
    /// Static so non-DI call sites can use it directly (mirrors the private helper this replaces,
    /// AuthenticationService.MaskMobile) — <see cref="IMobileProtectionService.Mask"/> forwards to
    /// this same implementation for DI callers holding only the interface.
    /// </summary>
    public static string Mask(string plainMobile) =>
        plainMobile.Length <= 4
            ? new string('x', plainMobile.Length)
            : new string('x', plainMobile.Length - 4) + plainMobile[^4..];
}
