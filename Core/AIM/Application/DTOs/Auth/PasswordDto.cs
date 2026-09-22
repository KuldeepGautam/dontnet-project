namespace UBIS.Services.Aim.Application.DTOs.Auth;

using UBIS.Services.Aim.Application.Validation;

/// <summary>
/// Request DTO for changing user password.
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// Current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password (must meet password policy requirements).
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of new password (must match NewPassword).
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO to complete a login that was blocked by a forced password reset
/// (User.PasswordResetRequired — e.g. a first-time login with the DBA-provisioned default
/// password). No prior session/JWT is required or expected: by definition none exists yet, since
/// normal login never finished. The caller re-proves identity with the current (default) password
/// instead of a bearer token.
/// </summary>
public class ChangeDefaultPasswordRequestDto
{
    public string UserName { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;

    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for password change operation.
/// </summary>
public class ChangePasswordResultDto
{
    /// <summary>
    /// Indicates if password change was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message explaining the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when password was changed (UTC).
    /// </summary>
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Request DTO for forgot password / password reset initiation.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email address associated with the user account.
    /// </summary>
    [GovernmentEmail]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional username for additional verification.
    /// </summary>
    public string? UserName { get; set; }
}

/// <summary>
/// Response DTO for forgot password request.
/// </summary>
public class ForgotPasswordResultDto
{
    /// <summary>
    /// Indicates if reset token was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message (generic for security - doesn't reveal if email exists).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reset token (should be sent via email in production).
    /// </summary>
    public string? ResetToken { get; set; }
}

/// <summary>
/// Request DTO for resetting password with token.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// Password reset token received via email.
    /// </summary>
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// New password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of new password.
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for password reset operation.
/// </summary>
public class ResetPasswordResultDto
{
    /// <summary>
    /// Indicates if password reset was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message explaining the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when password was reset (UTC).
    /// </summary>
    public DateTime ResetAt { get; set; }
}

/// <summary>
/// Reflects <c>PasswordResetOtp</c> config so the Login page's forgot-password dialog can decide,
/// before asking for a username, whether to show the request form at all or go straight to
/// "contact your administrator". Purely config-derived — no DB access, safe to expose anonymously.
/// Added 2026-07-22.
/// </summary>
public class PasswordResetAvailabilityDto
{
    public bool EmailEnabled { get; set; }

    public bool MobileEnabled { get; set; }

    public bool Available => EmailEnabled || MobileEnabled;
}

/// <summary>
/// Request DTO to start the OTP-based forgot-password flow (added 2026-07-22). Identifies the
/// account by username (not a typed email) so the OTP always goes to the ALREADY-registered
/// Email/Mobile on file — same anti-hijack reasoning as the AIM_Addendum_OTP_PasswordReset.claude
/// spec's §4.2 note on the (superseded) first-time-setup flow.
/// </summary>
public class PasswordResetOtpRequestDto
{
    public string UserName { get; set; } = string.Empty;
}

/// <summary>Response DTO for <see cref="PasswordResetOtpRequestDto"/>. Added 2026-07-22.</summary>
public class PasswordResetOtpRequestResultDto
{
    /// <summary>False when both PasswordResetOtp channels are disabled — caller should show
    /// "contact your administrator" and never even show a username field.</summary>
    public bool Available { get; set; } = true;

    /// <summary>Generic (anti-enumeration) success/failure of the request itself.</summary>
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Masked (e.g. "ag***@nic.in"), only present when Available and the account has an
    /// email on file — never the real address.</summary>
    public string? MaskedEmail { get; set; }

    /// <summary>Masked (e.g. "******7890"), only present when Available and the account has a
    /// mobile number on file.</summary>
    public string? MaskedMobile { get; set; }
}

/// <summary>Request DTO to verify the forgot-password OTP. Added 2026-07-22.</summary>
public class VerifyPasswordResetOtpRequestDto
{
    public string UserName { get; set; } = string.Empty;

    public string Otp { get; set; } = string.Empty;
}

/// <summary>Response DTO for <see cref="VerifyPasswordResetOtpRequestDto"/>. On success,
/// <see cref="ResetToken"/> is handed straight to <c>POST /api/authentication/reset-password</c>
/// (the existing endpoint) — safe to return directly here (unlike ForgotPasswordResultDto's
/// dev-only ResetToken) since OTP proof-of-possession already happened in THIS same request.
/// Added 2026-07-22.</summary>
public class VerifyPasswordResetOtpResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ResetToken { get; set; }
}
