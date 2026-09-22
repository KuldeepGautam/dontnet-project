namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>
/// Section 3 Profile Dashboard fields, sourced from the bridged legacy record when one exists.
/// Field-to-column mapping is literal per the compliance brief: Username=LoginId,
/// Department=DepartmentId, Email=emailld, "Created By"=the record's own UserId (the legacy
/// schema has no separate CreatedBy column). Added 2026-07-10.
/// </summary>
public class LegacyProfileDto
{
    public bool HasLegacyRecord { get; set; }

    public string? Username { get; set; }

    public string? Role { get; set; }

    public int? DepartmentId { get; set; }

    public string? Email { get; set; }

    /// <summary>Masked (e.g. "xxxxxx3210"), never the real number — see
    /// <c>IMobileProtectionService</c>. Renamed from plaintext <c>Mobile</c> 2026-08-17
    /// (Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    public DateTime? UserCreationDate { get; set; }

    public int? CreatedBy { get; set; }

    /// <summary>Added for the UserProfile microservice — already a real column on User, just never exposed here before.</summary>
    public DateTime? LastLoginDate { get; set; }
}
