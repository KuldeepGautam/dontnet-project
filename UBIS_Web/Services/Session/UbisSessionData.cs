namespace UBIS.Web.Services.Session;

using UBIS.Web.Services.Clients;

/// <summary>
/// Everything sensitive lives here, server-side, in the Redis-backed ASP.NET Core Session.
/// The browser's auth cookie only ever carries an opaque session identifier — never this data.
/// </summary>
public class UbisSessionData
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string AimSessionId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    /// <summary>The caller's dbo.M_Role.RoleId (added 2026-07-13) — used for the admin-gate check
    /// instead of the earlier invented AIM:UserAdmin:R claim.</summary>
    public int RoleId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public DateTime TokenExpiresAtUtc { get; set; }

    /// <summary>FunctionIds this session has existence-based access to (added 2026-07-13 —
    /// replaces the earlier "FunctionCode:CRUDMatrix" string shape).</summary>
    public List<int> Permissions { get; set; } = new();

    public MenuFullResponseDto? Menu { get; set; }

    // -- Compliance snapshot (Section 2/4 interceptors), populated at login from AIM's
    // GET /api/users/compliance-status. Added 2026-07-10.

    /// <summary>True if AIM flagged this login as still using a system-assigned default/reset-pending password.</summary>
    public bool PasswordResetRequired { get; set; }

    public bool IsAccountFrozen { get; set; }

    public string? AllowedIpAddressOne { get; set; }

    public string? AllowedIpAddressTwo { get; set; }

    /// <summary>False when an admin has granted this user an IP-check bypass (M_Users.IPAuthFlag
    /// != "Y") — ComplianceInterceptorFilter's hardware IP re-check only enforces when this is
    /// true (and Security:IsDevelopmentTime is off). Added 2026-07-23.</summary>
    public bool RequireIpValidation { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public DateTime? EmailLastValidatedAtUtc { get; set; }

    /// <summary>True once the user has dismissed/actioned this session's staleness warning (Section 4), so it isn't re-shown on every navigation.</summary>
    public bool ComplianceWarningAcknowledged { get; set; }

    // -- Security-stamp baseline (added 2026-08-10): captured ONCE, the first time
    // RefreshComplianceStatus/ResolveMyRole succeed after login, and never overwritten again -
    // unlike RoleId/PasswordChangedAtUtc above, which those same calls keep refreshing to their
    // latest live value on every subsequent call. SecurityStampStatus compares a fresh live read
    // against these frozen baselines to detect a password/role change made directly in the DB
    // (out-of-band, not through this app), forcing re-login the same way a self-service password
    // change already does.

    public DateTime? BaselinePasswordChangedAtUtc { get; set; }

    public int? BaselineRoleId { get; set; }
}
