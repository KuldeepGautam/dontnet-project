namespace UBIS.Web.Models;

using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(20)]
    [Display(Name = "Username / Email")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Added 2026-07 per client MOM ("Username, Password, and CAPTCHA are required").</summary>
    [Required(ErrorMessage = "Please enter the characters shown in the image.")]
    [Display(Name = "Security Code")]
    public string CaptchaAnswer { get; set; } = string.Empty;

    /// <summary>Display-only — a ready-to-embed <c>data:image/svg+xml;base64,...</c> URI, not bound from the form.</summary>
    public string? CaptchaImageSvg { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Non-error confirmation banner (e.g. "password reset — sign in with your new password"). Added 2026-07-22.</summary>
    public string? InfoMessage { get; set; }

    public string? ReturnUrl { get; set; }
}
