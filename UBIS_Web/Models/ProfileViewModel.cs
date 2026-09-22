namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;

public class ProfileViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // -- Section 3 Profile Dashboard fields (added 2026-07-10). Populated from AIM's
    // legacy-profile lookup; blank/null when the account has no bridged legacy record.
    public int? DepartmentId { get; set; }

    /// <summary>Masked (e.g. "xxxxxx3210"), never the real number. Renamed from plaintext
    /// <c>Mobile</c> 2026-08-17 (AIM Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    public DateTime? UserCreationDate { get; set; }

    public int? CreatedBy { get; set; }

    public bool HasLegacyRecord { get; set; }

    /// <summary>"My Assigned Statements/Profiles" (added 2026-07) — Statements the user owns or
    /// is explicitly assigned to for the current session's financial year.</summary>
    public List<UBIS.Web.Services.Clients.AssignedStatementDto> AssignedStatements { get; set; } = new();

    // -- Section 3 self-service change-request sub-form (Mobile/IP only — see
    // COMPLIANCE_NOTES.md for why Email isn't included: the legacy UserIPrequest table has
    // no Email column to hold a pending value).
    // Optional field - the client only submits it when they actually want to change their mobile
    // number (IP-only changes leave this blank), so the pattern allows an empty value through and
    // only enforces "exactly 10 digits" once something is actually entered.
    [Display(Name = "New Mobile Number")]
    [RegularExpression(@"^(\d{10})?$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
    public string? RequestedMobile { get; set; }

    // Same reasoning/pattern as UserProfileViewModel.IPAddress1/2 - optional, but if provided must
    // be a real IPv4 address (4 octets 0-255 separated by exactly 3 dots), not just "digits and
    // dots" (which would still let something like 999.999.999.999 through).
    [RegularExpression(@"^$|^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$",
        ErrorMessage = "Enter a valid IPv4 address (e.g. 192.168.1.1).")]
    [Display(Name = "New Primary IP Address")]
    public string? RequestedIPAddressOne { get; set; }

    [RegularExpression(@"^$|^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$",
        ErrorMessage = "Enter a valid IPv4 address (e.g. 192.168.1.1).")]
    [Display(Name = "New Secondary IP Address")]
    public string? RequestedIPAddressTwo { get; set; }

    public string? ChangeRequestStatusMessage { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmNewPassword { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
