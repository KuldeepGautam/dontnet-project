namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;
using UBIS.Web.Services.Clients;

public class UserProfileViewModel
{
    // -- Non-editable
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public DateTime? LastLoginDate { get; set; }

    // -- Merged in from the old Profile Dashboard (UserController.EditProfile), client requirement
    // 2026-09-03: "All edit profile functioning should [be] in User Profile Page". Populated from
    // AIM's legacy-profile lookup; blank/null when the account has no bridged legacy record.
    public int? DepartmentId { get; set; }

    public DateTime? UserCreationDate { get; set; }

    public int? CreatedBy { get; set; }

    public bool HasLegacyRecord { get; set; }

    /// <summary>"My Assigned Statements/Profiles" — Statements the user owns or is explicitly
    /// assigned to for the current session's financial year.</summary>
    public List<AssignedStatementDto> AssignedStatements { get; set; } = new();

    /// <summary>Full IP change-request history for the new grid (client requirement 2026-09-03:
    /// "a grid showing IP request number, list of IP addresses and date of request raised,
    /// Status") — distinct from LatestRequestStatus/Date/ApproveDate below, which stay as the
    /// "your most recent request" summary banner above the Raise-request form.</summary>
    public List<IpRequestHistoryItemDto> IpRequestHistory { get; set; } = new();

    // -- Editable (IP change goes through admin-approval request; Email/Mobile save immediately)
    // A real IPv4 address is 4 numeric octets (0-255) separated by exactly 3 dots, e.g.
    // "192.168.1.1" - the pattern below enforces the octet range (not just "digits and dots",
    // which would still let something like "999.999.999.999" through) and allows an empty value
    // since either field can be left blank when only changing the other one.
    [RegularExpression(@"^$|^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$",
        ErrorMessage = "Enter a valid IPv4 address (e.g. 192.168.1.1).")]
    [Display(Name = "IP Address 1")]
    public string? IPAddress1 { get; set; }

    [RegularExpression(@"^$|^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$",
        ErrorMessage = "Enter a valid IPv4 address (e.g. 192.168.1.1).")]
    [Display(Name = "IP Address 2")]
    public string? IPAddress2 { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    /// <summary>Masked current value (e.g. "xxxxxx3210"), display-only — never prefilled into the
    /// editable <see cref="Mobile"/> input below. Renamed from plaintext <c>Mobile</c> 2026-08-17
    /// (AIM Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    /// <summary>New mobile number, typed fresh each time — deliberately left blank rather than
    /// prefilled with the current (now server-side-encrypted) number, so this can no longer just
    /// echo back a decrypted value into the page. Optional (same "blank = no change" pattern as
    /// IPAddress1/2 above) — AIM's UpdateContact endpoint already treats a blank Mobile as "don't
    /// change it".</summary>
    [RegularExpression(@"^$|^\d{10}$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
    [Display(Name = "New Mobile Number")]
    public string? Mobile { get; set; }

    // -- Latest request status, for display only
    public IpRequestStatus? LatestRequestStatus { get; set; }

    public DateTime? LatestRequestDate { get; set; }

    public DateTime? LatestApproveDate { get; set; }

    // -- Change Password (merged in from UserController.EditProfile). Uses its own
    // Password*/StatusMessage pair, separate from the general StatusMessage/StatusIsError below,
    // so a Contact/IP-request submission on this same page never clobbers a password-form message
    // (and vice versa) when the page redisplays after one of the other forms posts.
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

    public string? PasswordStatusMessage { get; set; }

    public bool PasswordStatusIsError { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
