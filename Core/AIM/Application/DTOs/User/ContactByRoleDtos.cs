namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>One user matching a role-name lookup, with just enough contact detail for the caller
/// to send a notification. Added 2026-08-14 for the Pre-Budget "Add Allocation" screen's
/// Email/SMS-to-recipients feature.</summary>
public class ContactByRoleDto
{
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Real (decrypted) mobile number — this DTO's whole purpose is letting the caller
    /// actually dispatch an SMS/email to these recipients, so it is deliberately NOT masked here
    /// (unlike the profile-display DTOs). Added 2026-08-17 (Workstream 7) doc note: decrypted via
    /// <c>IMobileProtectionService.Unprotect</c> from <c>User.EncryptedMobile</c>.</summary>
    public string? Mobile { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

/// <summary>Wrapper for GET /api/users/contacts-by-role. Added 2026-08-14.</summary>
public class ContactsByRoleResultDto
{
    public List<ContactByRoleDto> Contacts { get; set; } = new();
}
