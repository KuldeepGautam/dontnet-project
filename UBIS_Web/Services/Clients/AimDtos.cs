namespace UBIS.Web.Services.Clients;

// Local copies of AIM's response shapes. AIM has no shared contracts project (every
// microservice in Core/ duplicates its own DTOs), so UBIS_Web follows the same convention.

/// <summary>Mirrors AIM's AppSettingsSnapshot record (dbo.AppSettings' 4 well-known flags) - see api/app-settings.</summary>
public class AppSettingsSnapshotDto
{
    public bool EnableEmail { get; set; }
    public bool EnableIPLogging { get; set; }
    public bool EnableHttps { get; set; }
    public bool EnableCertificate { get; set; }
    public bool EnableSms { get; set; }

    // dbo.AppSettingsInt.IdleTimer (client requirement 2026-09-04) - default matches AIM's own
    // AppSettingsReader.GetAllAsync fallback, only relevant if AIM/the DB is unreachable.
    public int IdleTimerMinutes { get; set; } = 15;
}

public class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool PasswordResetRequired { get; set; }

    public DateTime TokenExpiresAt { get; set; }

    /// <summary>FunctionIds this session has existence-based access to (added 2026-07-13 —
    /// replaces the earlier "FunctionCode:CRUDMatrix" string shape).</summary>
    public List<int> Permissions { get; set; } = new();

    /// <summary>Mirrors AIM's login MFA toggle (added 2026-07). See <see cref="VerifyLoginOtpRequestDto"/>.</summary>
    public bool MfaRequired { get; set; }

    public int? MfaChallengeUserId { get; set; }
}

/// <summary>Mirrors AIM's VerifyLoginOtpRequestDto. Added 2026-07.</summary>
public class VerifyLoginOtpRequestDto
{
    public int ChallengeUserId { get; set; }

    public string Otp { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's ResendLoginOtpRequestDto. Added 2026-07.</summary>
public class ResendLoginOtpRequestDto
{
    public int ChallengeUserId { get; set; }
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class AimErrorDto
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }
}

public class UserRoleDto
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public RoleDetailDto Role { get; set; } = new();

    public string FinancialYear { get; set; } = string.Empty;

    public List<FunctionPermissionDto> FunctionPermissions { get; set; } = new();
}

public class RoleDetailDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's FunctionPermissionDto — existence-based access, no CRUD granularity
/// (the real dbo.M_MapRoleFunction table has no such columns). Added 2026-07-13, replaces the
/// earlier invented CanCreate/CanRead/CanUpdate/CanDelete/PermissionMatrix shape.</summary>
public class FunctionPermissionDto
{
    public int FunctionId { get; set; }

    public string FunctionName { get; set; } = string.Empty;
}

public class UserEmailLookupDto
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's LegacyProfileDto. Added 2026-07-10.</summary>
public class LegacyProfileDto
{
    public bool HasLegacyRecord { get; set; }

    public string? Username { get; set; }

    public string? Role { get; set; }

    public int? DepartmentId { get; set; }

    public string? Email { get; set; }

    /// <summary>Masked (e.g. "xxxxxx3210"), never the real number. Renamed from plaintext
    /// <c>Mobile</c> 2026-08-17 (AIM Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    public DateTime? UserCreationDate { get; set; }

    public int? CreatedBy { get; set; }
}

/// <summary>Mirrors AIM's SubmitChangeRequestDto. Added 2026-07-10.</summary>
public class SubmitChangeRequestDto
{
    public string? NewMobile { get; set; }

    public string? NewIPAddressOne { get; set; }

    public string? NewIPAddressTwo { get; set; }

    public int? DemandId { get; set; }
}

/// <summary>Mirrors AIM's ChangeRequestStatusDto. Added 2026-07-10.</summary>
public class ChangeRequestStatusDto
{
    public int? RowId { get; set; }

    public string? ApproveFlag { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    public bool JustApproved { get; set; }
}

/// <summary>Mirrors AIM's ComplianceStatusDto. Added 2026-07-10.</summary>
public class ComplianceStatusDto
{
    public bool IsAccountFrozen { get; set; }

    public string? AllowedIpAddressOne { get; set; }

    public string? AllowedIpAddressTwo { get; set; }

    /// <summary>Mirrors AIM's ComplianceStatusDto.RequireIpValidation — false means an admin has
    /// granted this user (M_Users.IPAuthFlag != "Y") a bypass from hardware IP-binding. Added 2026-07-23.</summary>
    public bool RequireIpValidation { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public DateTime? EmailLastValidatedAtUtc { get; set; }

    /// <summary>Mirrors AIM's ComplianceStatusDto.RoleId (added 2026-08-10) - null unless financialYear was passed to the request.</summary>
    public int? RoleId { get; set; }
}

/// <summary>Mirrors AIM's ActiveSessionDto - Active Session Monitor row. Added 2026-08-10.</summary>
public class ActiveSessionDto
{
    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public DateTime LoginTimeUtc { get; set; }

    public string? IPAddress { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime LastActivityUtc { get; set; }

    public double? TimeToLiveSeconds { get; set; }
}

/// <summary>Mirrors AIM's PendingChangeRequestDto. Added 2026-07.</summary>
public class PendingChangeRequestDto
{
    public int RowId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? RequestedMobile { get; set; }

    public string? RequestedIPAddressOne { get; set; }

    public string? RequestedIPAddressTwo { get; set; }

    public DateTime RequestDate { get; set; }
}

/// <summary>Mirrors AIM's AdminChangeRequestListDto. Added 2026-07.</summary>
public class AdminChangeRequestListDto
{
    public List<PendingChangeRequestDto> Items { get; set; } = new();
}

/// <summary>Mirrors AIM's AssignedStatementDto. Added 2026-07.</summary>
public class AssignedStatementDto
{
    public int StmtId { get; set; }

    public string? StmtNo { get; set; }

    public string? StmtName { get; set; }
}

/// <summary>Mirrors AIM's AssignedStatementsDto. Added 2026-07.</summary>
public class AssignedStatementsDto
{
    public List<AssignedStatementDto> Statements { get; set; } = new();
}

/// <summary>Mirrors AIM's DemandDto (GET api/users/demands). Added 2026-07-23.</summary>
public class DemandDto
{
    public int DemandId { get; set; }

    public int DemandNo { get; set; }

    public string DemandName { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's MyDemandsDto. Added 2026-07-23.</summary>
public class MyDemandsDto
{
    public List<DemandDto> Demands { get; set; } = new();
}

/// <summary>Mirrors AIM's FinancialYearOptionDto (dbo.M_FinancialYear). Added 2026-07-30; AppId
/// added 2026-09-08 (each app now has its own current-year flag - see AIM's FinancialYear.cs
/// entity for the full design note).</summary>
public class FinancialYearOptionDto
{
    public int Id { get; set; }
    public string YearRange { get; set; } = string.Empty;
    public string BudgetType { get; set; } = string.Empty;
    public bool IsCurrentYear { get; set; }
    public int BudgetCycle { get; set; }
    public int? AppId { get; set; }
}

/// <summary>Mirrors AIM's ForgotPasswordRequestDto. Added 2026-07-22.</summary>
public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }
}

/// <summary>Mirrors AIM's ForgotPasswordResultDto. Deliberately NOT including AIM's dev-only
/// <c>ResetToken</c> field here — this app must never surface a live reset token to the browser
/// that requested it, only the generic confirmation message.</summary>
public class ForgotPasswordResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's ResetPasswordRequestDto. Added 2026-07-22.</summary>
public class ResetPasswordRequestDto
{
    public string ResetToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's ResetPasswordResultDto. Added 2026-07-22.</summary>
public class ResetPasswordResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}

/// <summary>Mirrors the anonymous shape AIM's <c>GET /api/authentication/validate-reset-token/{token}</c> returns on success. Added 2026-07-22.</summary>
public class ValidateResetTokenResultDto
{
    public bool Valid { get; set; }

    public int? UserId { get; set; }
}

/// <summary>Mirrors AIM's PasswordResetAvailabilityDto — drives the Login page's forgot-password
/// dialog: whether to show the request form at all. Added 2026-07-22.</summary>
public class PasswordResetAvailabilityDto
{
    public bool EmailEnabled { get; set; }

    public bool MobileEnabled { get; set; }

    public bool Available { get; set; }
}

/// <summary>Mirrors AIM's PasswordResetOtpRequestDto. Added 2026-07-22.</summary>
public class PasswordResetOtpRequestDto
{
    public string UserName { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's PasswordResetOtpRequestResultDto. Added 2026-07-22.</summary>
public class PasswordResetOtpRequestResultDto
{
    public bool Available { get; set; } = true;

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? MaskedEmail { get; set; }

    public string? MaskedMobile { get; set; }
}

/// <summary>Mirrors AIM's VerifyPasswordResetOtpRequestDto. Added 2026-07-22.</summary>
public class VerifyPasswordResetOtpRequestDto
{
    public string UserName { get; set; } = string.Empty;

    public string Otp { get; set; } = string.Empty;
}

/// <summary>Mirrors AIM's VerifyPasswordResetOtpResultDto. Added 2026-07-22.</summary>
public class VerifyPasswordResetOtpResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ResetToken { get; set; }
}
