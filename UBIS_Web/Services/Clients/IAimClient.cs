namespace UBIS.Web.Services.Clients;

public interface IAimClient
{
    /// <summary>Rows from the central dbo.M_FinancialYear master for one app, newest first — for the Login page's dropdown. Added 2026-07-30 (was a distinct-M_User.FinancialYear fallback before); appId param added 2026-09-08 (each app now has its own current-year flag - see AIM's FinancialYear.cs entity for the design note).</summary>
    Task<ApiCallResult<List<FinancialYearOptionDto>>> GetFinancialYearOptionsAsync(int appId, CancellationToken ct = default);

    Task<ApiCallResult<LoginResultDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

    Task<ApiCallResult<LoginResultDto>> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task<ApiCallResult<object?>> LogoutAsync(string sessionId, CancellationToken ct = default);

    Task<ApiCallResult<object?>> LogoutAllSessionsAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<UserRoleDto>> GetMyRoleAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<UserEmailLookupDto>> GetEmailAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<object?>> ChangePasswordAsync(string bearerToken, ChangePasswordRequestDto request, CancellationToken ct = default);

    /// <summary>Backs Section 2/4 interceptors: freeze/IP-binding/staleness snapshot. Added 2026-07-10.</summary>
    Task<ApiCallResult<ComplianceStatusDto>> GetComplianceStatusAsync(string bearerToken, string? financialYear = null, CancellationToken ct = default);

    /// <summary>Section 3 Profile Dashboard fields. Added 2026-07-10.</summary>
    Task<ApiCallResult<LegacyProfileDto>> GetLegacyProfileAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>Submits a self-service Mobile/IP change request. Added 2026-07-10.</summary>
    Task<ApiCallResult<object?>> SubmitChangeRequestAsync(string bearerToken, SubmitChangeRequestDto request, CancellationToken ct = default);

    /// <summary>Polled by the remote kill-switch banner. Added 2026-07-10.</summary>
    Task<ApiCallResult<ChangeRequestStatusDto>> GetChangeRequestStatusAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<object?>> SendSessionHeartbeatAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<List<ActiveSessionDto>>> GetActiveSessionsAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>Admin: revokes every active session for a user (FR-006, added 2026-08-21) — their
    /// existing token(s) stop working immediately across every JWT-validating service.</summary>
    Task<ApiCallResult<object?>> ForceLogoutUserAsync(string bearerToken, int userId, CancellationToken ct = default);

    /// <summary>Completes a login paused for MFA. Added 2026-07.</summary>
    Task<ApiCallResult<LoginResultDto>> VerifyLoginOtpAsync(VerifyLoginOtpRequestDto request, CancellationToken ct = default);

    /// <summary>Re-sends the OTP for an in-progress login MFA challenge. Added 2026-07.</summary>
    Task<ApiCallResult<object?>> ResendLoginOtpAsync(ResendLoginOtpRequestDto request, CancellationToken ct = default);

    /// <summary>Admin: lists pending Mobile/IP change requests. Added 2026-07.</summary>
    Task<ApiCallResult<AdminChangeRequestListDto>> GetPendingChangeRequestsAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>Admin: approves a pending change request. Added 2026-07.</summary>
    Task<ApiCallResult<object?>> ApproveChangeRequestAsync(string bearerToken, int rowId, CancellationToken ct = default);

    /// <summary>Admin: rejects a pending change request. Added 2026-07.</summary>
    Task<ApiCallResult<object?>> RejectChangeRequestAsync(string bearerToken, int rowId, CancellationToken ct = default);

    /// <summary>"My Assigned Statements/Profiles" for the Profile page. Added 2026-07.</summary>
    Task<ApiCallResult<AssignedStatementsDto>> GetAssignedStatementsAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    /// <summary>Requests a password-reset token for the given email. Always returns a generic
    /// success (AIM never reveals whether the account exists). Added 2026-07-22.</summary>
    Task<ApiCallResult<ForgotPasswordResultDto>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);

    /// <summary>Validates a password-reset token before showing the "set a new password" form. Added 2026-07-22.</summary>
    Task<ApiCallResult<ValidateResetTokenResultDto>> ValidateResetTokenAsync(string resetToken, CancellationToken ct = default);

    /// <summary>Completes a password reset using the token from <see cref="ForgotPasswordAsync"/>. Added 2026-07-22.</summary>
    Task<ApiCallResult<ResetPasswordResultDto>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);

    /// <summary>Whether the Login page's forgot-password dialog can offer OTP-based reset right now. Added 2026-07-22.</summary>
    Task<ApiCallResult<PasswordResetAvailabilityDto>> GetPasswordResetOtpAvailabilityAsync(CancellationToken ct = default);

    /// <summary>Starts the OTP-based forgot-password flow. Added 2026-07-22.</summary>
    Task<ApiCallResult<PasswordResetOtpRequestResultDto>> RequestPasswordResetOtpAsync(PasswordResetOtpRequestDto request, CancellationToken ct = default);

    /// <summary>Verifies the forgot-password OTP. Added 2026-07-22.</summary>
    Task<ApiCallResult<VerifyPasswordResetOtpResultDto>> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequestDto request, CancellationToken ct = default);

    /// <summary>"My Demands" for the Pre-Budget Meeting module's Demand-selection dropdown. Added 2026-07-23.</summary>
    Task<ApiCallResult<MyDemandsDto>> GetMyDemandsAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    /// <summary>dbo.AppSettings' 4 well-known flags (EnableEmail/EnableIPLogging/EnableHttps/EnableCertificate) -
    /// UBIS_Web has no direct DB access, so it reads these via AIM. Added 2026-08-07.</summary>
    Task<ApiCallResult<AppSettingsSnapshotDto>> GetAppSettingsAsync(CancellationToken ct = default);

    Task<ApiCallResult<object?>> UpdateIdleTimerMinutesAsync(string bearerToken, int minutes, CancellationToken ct = default);
}
