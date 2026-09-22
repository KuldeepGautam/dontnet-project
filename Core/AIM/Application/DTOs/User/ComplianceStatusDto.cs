namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>
/// Consolidated compliance/security snapshot for the caller — backs the Section
/// 2 interceptors (IP binding, account freeze) and Section 4 (password/email staleness
/// warning) in UBIS_Web, resolved server-side once at login instead of many small round
/// trips. Added 2026-07-10; sourced directly from <c>dbo.M_Users</c> as of 2026-07-13
/// (no more separate legacy-record bridge — <c>User.UserId</c> is the same int identity).
/// </summary>
public class ComplianceStatusDto
{
    /// <summary>True if <c>M_Users.IsActive</c> is false (an admin has disabled this account) —
    /// replaces the legacy UserFreez/UserMFFreez two-flag check 2026-08-17.</summary>
    public bool IsAccountFrozen { get; set; }

    /// <summary>First allowed hardware IP (M_Users.IPadres1).</summary>
    public string? AllowedIpAddressOne { get; set; }

    /// <summary>Second allowed hardware IP (M_Users.IPadres2).</summary>
    public string? AllowedIpAddressTwo { get; set; }

    /// <summary>True when <c>M_Users.IPAuthFlag</c> is "Y" — an admin has NOT granted this user an
    /// IP-check bypass, so UBIS_Web's per-request hardware IP-binding re-check should actually
    /// enforce for them (subject to Security:IsDevelopmentTime still gating the whole check).
    /// Added 2026-07-23.</summary>
    public bool RequireIpValidation { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public DateTime? EmailLastValidatedAtUtc { get; set; }

    /// <summary>Caller's current dbo.M_MapUserRole.RoleId for FinancialYear (added 2026-08-10) - lets
    /// UBIS_Web detect an out-of-band role change (made directly in the DB, not via the app) and
    /// force re-login, the same way PasswordChangedAtUtc already detects an out-of-band password
    /// change. Null if FinancialYear wasn't supplied on the request.</summary>
    public int? RoleId { get; set; }
}
