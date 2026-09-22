namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>Added 2026-07-22 alongside the designer's login redesign. Wired to AIM's real
/// forgot-password/reset-password endpoints; see ResetPasswordViewModel for the follow-on step.</summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Enter your registered email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Registered Email Address")]
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? InfoMessage { get; set; }

    public string? ErrorMessage { get; set; }
}
