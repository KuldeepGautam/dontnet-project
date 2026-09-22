namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>Self-service direct update of Email/Mobile — unlike IP/legacy Mobile change requests, these save immediately, no admin approval.</summary>
public class UpdateContactRequestDto
{
    public string? Email { get; set; }

    /// <summary>Plaintext 10-digit mobile number as typed by the caller — encrypted server-side
    /// (<c>IMobileProtectionService.Protect</c>) before being stored in <c>User.EncryptedMobile</c>.
    /// Never persisted or logged as-is. Added 2026-08-17 (Workstream 7): this is a write-only DTO,
    /// so unlike the read-side DTOs it keeps the plaintext field name/shape.</summary>
    public string? Mobile { get; set; }
}
