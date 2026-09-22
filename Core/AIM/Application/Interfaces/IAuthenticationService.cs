namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.DTOs.Auth;

/// <summary>
/// Service interface for authentication operations.
/// Handles login, password management, and session lifecycle.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username, password, and financial year.
    /// Enforces brute force protection, IP binding, and role-based access.
    /// </summary>
    /// <param name="request">Login credentials and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login result with JWT token or error details.</returns>
    Task<Result<LoginResultDto>> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rows from the central dbo.M_FinancialYear master (added 2026-07-30) for one app
    /// (<paramref name="appId"/>, added 2026-09-08 - FK to dbo.M_AppName, each app now has its own
    /// current-year flag), newest first — lets the Login page (and any other module) offer a real
    /// dropdown, with <see cref="FinancialYearOptionDto.IsCurrentYear"/> marking the default
    /// selection. Replaces the old distinct-M_Users.FinancialYear fallback. Anonymous (runs
    /// before login), so this must never expose anything beyond the year/budget-type metadata itself.
    /// </summary>
    Task<List<FinancialYearOptionDto>> GetFinancialYearOptionsAsync(int appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid (unexpired, unused) refresh token for a new signed JWT
    /// and a rotated refresh token, without requiring credentials again.
    /// </summary>
    /// <param name="request">The refresh token issued at login or the previous refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New login result with fresh token/refresh token, or error.</returns>
    Task<Result<LoginResultDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a login that was paused for MFA (added 2026-07) by verifying the OTP and, on
    /// success, issuing the JWT/refresh token/Redis session exactly as <see cref="AuthenticateAsync"/>
    /// would have if MFA were disabled.
    /// </summary>
    Task<Result<LoginResultDto>> VerifyLoginOtpAsync(
        VerifyLoginOtpRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-sends the OTP for an in-progress login MFA challenge (added 2026-07).
    /// </summary>
    Task<Result<bool>> ResendLoginOtpAsync(
        ResendLoginOtpRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user password after validating current password.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Password change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Password change result or error.</returns>
    Task<Result<ChangePasswordResultDto>> ChangePasswordAsync(
        int userId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a login blocked by a forced password reset (User.PasswordResetRequired) — no
    /// prior session/JWT required, since normal login never finished for this user. Re-verifies
    /// the current (default) password directly, then — only if the account is actually flagged
    /// PasswordResetRequired — updates it and signs the user straight in, issuing a real
    /// Token/RefreshToken/SessionId exactly as <see cref="AuthenticateAsync"/> would.
    /// </summary>
    Task<Result<LoginResultDto>> ChangeDefaultPasswordAsync(
        ChangeDefaultPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates forgot password flow by generating reset token.
    /// Token should be sent via email in production.
    /// </summary>
    /// <param name="request">Forgot password request with email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reset token or generic success message.</returns>
    Task<Result<ForgotPasswordResultDto>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets password using reset token received via email.
    /// </summary>
    /// <param name="request">Password reset request with token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reset result or error.</returns>
    Task<Result<ResetPasswordResultDto>> ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates reset token and returns associated user if valid.
    /// </summary>
    /// <param name="resetToken">Token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User ID if token is valid, error otherwise.</returns>
    Task<Result<int>> ValidateResetTokenAsync(
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the OTP-based forgot-password flow can be offered at all right now (added
    /// 2026-07-22). No DB access beyond reading dbo.AppSettings.EnableSms via AppSettingsReader
    /// (cached) — Email reflects PasswordResetOtp:EmailEnabled config, Mobile reflects EnableSms
    /// (moved off config 2026-08-07).
    /// </summary>
    Task<PasswordResetAvailabilityDto> GetPasswordResetOtpAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the OTP-based forgot-password flow (added 2026-07-22): if at least one channel is
    /// enabled and the username resolves to a real account, generates and dispatches an OTP to
    /// the account's on-file Email/Mobile (never a caller-supplied address). Anti-enumeration:
    /// an unknown username gets the same generic success shape as a known one.
    /// </summary>
    Task<PasswordResetOtpRequestResultDto> RequestPasswordResetOtpAsync(
        PasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the forgot-password OTP (added 2026-07-22). On success, mints the same kind of
    /// short-lived reset token <see cref="ForgotPasswordAsync"/> does, for immediate use by
    /// <see cref="ResetPasswordAsync"/> in the same dialog session.
    /// </summary>
    Task<VerifyPasswordResetOtpResultDto> VerifyPasswordResetOtpAsync(
        VerifyPasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out user by invalidating session in Redis cache.
    /// </summary>
    /// <param name="sessionId">Session ID to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Logout result.</returns>
    Task<Result<bool>> LogoutAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies if session is still valid in Redis cache.
    /// </summary>
    /// <param name="sessionId">Session ID to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if session is valid, false otherwise.</returns>
    Task<bool> IsSessionValidAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
