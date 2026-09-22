namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>The page a "Forgot Password" email's reset link lands on. Added 2026-07-22.</summary>
public class ResetPasswordViewModel
{
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>False when the token is missing/invalid/expired — the form is hidden and
    /// <see cref="ErrorMessage"/> shown instead.</summary>
    public bool TokenValid { get; set; }

    [Required(ErrorMessage = "Enter a new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
