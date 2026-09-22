namespace UBIS.Services.Aim.Application.DTOs.Auth;

/// <summary>
/// Request DTO for user login with brute force protection and IP binding validation.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Username (max 20 characters).
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User password (will be verified against BCrypt hash).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Financial year for scoped access (e.g., '2024-2025').
    /// </summary>
    public string FinancialYear { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for successful login with JWT token and session information.
/// </summary>
public class LoginResultDto
{
    /// <summary>
    /// JWT token for authenticated requests (15-minute TTL).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Single-use refresh token; exchange it via POST /api/authentication/refresh
    /// for a new Token before this one expires.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Session ID embedded in the token for Redis tracking.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the authenticated user.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the authenticated user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if password reset is required on next login.
    /// </summary>
    public bool PasswordResetRequired { get; set; }

    /// <summary>
    /// Indicates token expiration time (UTC).
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }

    /// <summary>
    /// FunctionIds this session has existence-based access to (added 2026-07-13 — replaces the
    /// earlier "FunctionCode:CRUDMatrix" string shape, since the real schema has neither a
    /// FunctionCode column nor CRUD granularity).
    /// </summary>
    public List<int> Permissions { get; set; } = new();

    /// <summary>
    /// True when <c>Mfa:Enabled</c> is on and credentials/IP-binding passed but an OTP challenge
    /// must still be verified (added 2026-07). When true, <see cref="Token"/>/<see cref="RefreshToken"/>
    /// are empty — no session has been issued yet. Call <c>POST /api/authentication/login/verify-otp</c>
    /// with <see cref="MfaChallengeUserId"/> to complete login.
    /// </summary>
    public bool MfaRequired { get; set; }

    /// <summary>
    /// Opaque-enough identifier for the in-progress MFA challenge (only present when
    /// <see cref="MfaRequired"/> is true).
    /// </summary>
    public int? MfaChallengeUserId { get; set; }
}

/// <summary>
/// Request DTO to complete a login that was paused for MFA (added 2026-07).
/// </summary>
public class VerifyLoginOtpRequestDto
{
    public int ChallengeUserId { get; set; }
    public string Otp { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO to re-send the OTP for an in-progress login MFA challenge (added 2026-07).
/// </summary>
public class ResendLoginOtpRequestDto
{
    public int ChallengeUserId { get; set; }
}

/// <summary>
/// Request DTO to exchange a refresh token for a new JWT.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// The refresh token previously issued at login or by a prior refresh.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// One selectable row from dbo.M_FinancialYear (added 2026-07-30) — lets the Login page (and any
/// other module) render a real dropdown, with <see cref="IsCurrentYear"/> marking which one to
/// default-select, instead of the year being resolved invisibly server-side.
/// </summary>
public class FinancialYearOptionDto
{
    public int Id { get; set; }
    public string YearRange { get; set; } = string.Empty;
    public string BudgetType { get; set; } = string.Empty;
    public bool IsCurrentYear { get; set; }
    public int BudgetCycle { get; set; }

    /// <summary>Which app this row's current-year flag belongs to (added 2026-09-08) - echoed
    /// back for debuggability; callers already know which AppId they asked for.</summary>
    public int? AppId { get; set; }
}

/// <summary>
/// Response DTO for login failures with detailed error information.
/// </summary>
public class LoginErrorDto
{
    /// <summary>
    /// Error code (e.g., 'INVALID_CREDENTIALS', 'IP_BINDING_FAILURE', 'ACCOUNT_LOCKED').
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Additional details for debugging (only in development).
    /// </summary>
    public string? Details { get; set; }
}
