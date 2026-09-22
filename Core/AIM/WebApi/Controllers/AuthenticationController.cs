namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.Aim.Application.DTOs.Auth;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// API controller for authentication operations.
/// Provides endpoints for login, password management, and session handling.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Rows from the central dbo.M_FinancialYear master for one app, newest first — the Login
    /// page's dropdown source of truth (2026-07-30). Anonymous, since this runs before login;
    /// returns nothing beyond the year/budget-type metadata itself.
    ///
    /// <paramref name="appId"/> (added 2026-09-08, FK to dbo.M_AppName): required now that each
    /// app tracks its own current-year flag independently - see FinancialYear.cs's own comment
    /// for why this replaced an earlier synthetic-per-app-code idea.
    /// </summary>
    [HttpGet("financial-years")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialYears([FromQuery] int appId, CancellationToken cancellationToken = default)
    {
        var years = await _authenticationService.GetFinancialYearOptionsAsync(appId, cancellationToken);
        return Ok(years);
    }

    /// <summary>
    /// Authenticates user with username, password, and financial year.
    /// Enforces brute force protection, IP binding, and role-based access.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and session information on success.</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResultDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for user: {UserName}", request.UserName);

        var result = await _authenticationService.AuthenticateAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successful login for user: {UserName}", request.UserName);
            return Ok(result.Data);
        }

        return result.Error!.Code switch
        {
            "RATE_LIMIT_EXCEEDED" => StatusCode(StatusCodes.Status429TooManyRequests, result.Error),
            "IP_BINDING_FAILURE" => Forbid(),
            _ => Unauthorized(result.Error)
        };
    }

    /// <summary>
    /// Completes a login paused for MFA (see <see cref="Login"/>'s <c>MfaRequired</c> response).
    /// </summary>
    /// <param name="request">The challenge user id and the OTP the user entered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and session information on success.</returns>
    [HttpPost("login/verify-otp")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResultDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyLoginOtp(
        [FromBody] VerifyLoginOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.VerifyLoginOtpAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return Unauthorized(result.Error);
    }

    /// <summary>
    /// Re-sends the OTP for an in-progress login MFA challenge.
    /// </summary>
    /// <param name="request">The challenge user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login/resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendLoginOtp(
        [FromBody] ResendLoginOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.ResendLoginOtpAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { Message = "A new OTP has been sent." });
        }

        return Unauthorized(result.Error);
    }

    /// <summary>
    /// Exchanges a refresh token for a new JWT without requiring credentials again.
    /// </summary>
    /// <param name="request">The refresh token issued at login or a previous refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New JWT/refresh token pair, or 401 if the refresh token is invalid/expired.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResultDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.RefreshTokenAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return Unauthorized(result.Error);
    }

    /// <summary>
    /// Changes user password after validating current password.
    /// </summary>
    /// <param name="request">Password change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message or error.</returns>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChangePasswordResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized("User not authenticated.");
        }

        _logger.LogInformation("Password change request for user: {UserId}", userId);

        var result = await _authenticationService.ChangePasswordAsync(userId.Value, request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(result.Data);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Completes a login blocked by a forced password reset (see <see cref="Login"/>'s
    /// <c>PasswordResetRequired</c> response) — e.g. a first-time login with the DBA-provisioned
    /// default password. No prior session/JWT is sent or required, since normal login never
    /// finished for this user; the caller re-proves identity with the current (default) password.
    /// On success, signs the user straight in (same response shape as <see cref="Login"/>) so the
    /// frontend doesn't need a second round trip through /login with the new password.
    /// </summary>
    /// <param name="request">Username/financial year, current (default) password, and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and session information on success.</returns>
    [HttpPost("change-default-password")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangeDefaultPassword(
        [FromBody] ChangeDefaultPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forced password change attempt for user: {UserName}", request.UserName);

        var result = await _authenticationService.ChangeDefaultPasswordAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Forced password change completed for user: {UserName}", request.UserName);
            return Ok(result.Data);
        }

        return result.Error!.Code switch
        {
            "RATE_LIMIT_EXCEEDED" => StatusCode(StatusCodes.Status429TooManyRequests, result.Error),
            "IP_BINDING_FAILURE" => Forbid(),
            _ => Unauthorized(result.Error)
        };
    }

    /// <summary>
    /// Initiates forgot password flow.
    /// Generates reset token and sends via email (in production).
    /// </summary>
    /// <param name="request">Forgot password request with email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generic success message for security.</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ForgotPasswordResultDto))]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forgot password request for email: {Email}", request.Email);

        var result = await _authenticationService.ForgotPasswordAsync(request, cancellationToken);

        // Always return success for security (doesn't reveal if email exists)
        return Ok(result.Data);
    }

    /// <summary>
    /// So the Login page's forgot-password dialog can decide upfront whether to show the request
    /// form at all, before ever asking for a username (added 2026-07-22).
    /// </summary>
    [HttpGet("password-reset/availability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PasswordResetAvailabilityDto))]
    public async Task<IActionResult> GetPasswordResetOtpAvailability(CancellationToken ct) =>
        Ok(await _authenticationService.GetPasswordResetOtpAvailabilityAsync(ct));

    /// <summary>
    /// Starts the OTP-based forgot-password flow (added 2026-07-22): generates and dispatches an
    /// OTP to the account's on-file Email/Mobile, per whichever PasswordResetOtp channels are
    /// enabled. Always 200 (generic success shape) to avoid revealing account existence.
    /// </summary>
    [HttpPost("password-reset/request-otp")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PasswordResetOtpRequestResultDto))]
    public async Task<IActionResult> RequestPasswordResetOtp(
        [FromBody] PasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.RequestPasswordResetOtpAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Verifies the forgot-password OTP (added 2026-07-22) and, on success, returns a reset token
    /// for immediate use by <see cref="ResetPassword"/> in the same dialog session.
    /// </summary>
    [HttpPost("password-reset/verify-otp")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyPasswordResetOtpResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPasswordResetOtp(
        [FromBody] VerifyPasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Resets password using reset token received via email.
    /// </summary>
    /// <param name="request">Password reset request with token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message or error.</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResetPasswordResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset request received");

        var result = await _authenticationService.ResetPasswordAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Validates password reset token.
    /// </summary>
    /// <param name="resetToken">Reset token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User ID if valid, error otherwise.</returns>
    [HttpGet("validate-reset-token/{resetToken}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateResetToken(
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var result = await _authenticationService.ValidateResetTokenAsync(resetToken, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { Valid = true, UserId = result.Data });
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Logs out user by invalidating session.
    /// </summary>
    /// <param name="sessionId">Session ID to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromQuery] string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Session ID is required.");
        }

        _logger.LogInformation("Logout request for session: {SessionId}", sessionId);

        var result = await _authenticationService.LogoutAsync(sessionId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { Message = "Logged out successfully." });
        }

        return BadRequest(result.Error);
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
