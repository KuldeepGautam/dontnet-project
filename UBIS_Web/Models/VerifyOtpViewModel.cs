namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>Login MFA step (added 2026-07, default inert since <c>Mfa:Enabled</c> defaults to off in AIM).</summary>
public class VerifyOtpViewModel
{
    [Required(ErrorMessage = "Enter the 6-digit code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "The code is 6 digits.")]
    [Display(Name = "Verification Code")]
    public string Otp { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? InfoMessage { get; set; }

    /// <summary>Mirrors <c>Mfa:PhoneOtpEnabled</c> (default false — no SMS gateway provisioned
    /// yet, same gap AIM's own NotConfiguredSmsServiceClient documents). Controls whether the
    /// "Phone" method button on this page is selectable or shown disabled for future
    /// compatibility. Added 2026-07-22.</summary>
    public bool PhoneOtpEnabled { get; set; }
}
