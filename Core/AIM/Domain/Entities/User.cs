namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real DBA-owned <c>dbo.M_User</c> table (int identity) — replaces both the
/// earlier Guid-keyed <c>aim.Users</c> table and the <c>dbo.Users</c>/<c>LegacyUserRecord</c>
/// bridge: this is now the single, direct identity every other legacy table's int
/// <c>UserId</c>/<c>UsersId</c>/<c>StmtUserId</c> column already pointed at. Added 2026-07-13.
/// One row per **person** — consolidated 2026-08-03 from the earlier "one row per person per
/// financial year" model (which had 4,308 rows for only 567 distinct LoginIds). The per-year
/// association now lives in <see cref="UserFinancialYear"/> (<c>dbo.M_MapUserFY</c>) instead; see
/// <see cref="UserFinancialYears"/>. The old per-person-per-year row chain pointer (<c>PrevUserId</c>)
/// was dead weight post-consolidation and was dropped 2026-08-17 (archived first into
/// <c>dbo.M_User_ArchivedColumns_20260817</c>; see also <c>dbo.Legacy_UserConsolidationMap</c> for
/// the full old-UserId -> retained-UserId mapping). Table mapping is via Fluent API only (see
/// <c>UserConfiguration.cs</c>) — no <c>[Table]</c> Data Annotation here, since a stray one
/// previously held a malformed, pre-bracketed identifier string.
/// </summary>
public class User
{
    public int UserId { get; set; }

    /// <summary>Login username (column renamed from LoginId 2026-08-19 — Workstream 8).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt-12 password hash (legacy column name is <c>Password</c>).</summary>
    public string? PasswordHash { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public DateTime? LastPasswordChangeDate { get; set; }

    public DateTime? FirstFailLoginAttemptDate { get; set; }

    public int? FailedLoginAttempts { get; set; }

    public string? OldPassword1 { get; set; }

    public string? OldPassword2 { get; set; }

    public string? OldPassword3 { get; set; }

    public string? InitialUserLevel { get; set; }

    /// <summary>
    /// Data-Protection-encrypted ciphertext (base64) of this user's 10-digit mobile number —
    /// replaces the plaintext <c>Mobile</c> column 2026-08-17 (Workstream 7: Mobile encryption +
    /// masking). Never expose this raw value to a caller; decrypt via
    /// <c>IMobileProtectionService.Unprotect</c> only where the real number is actually needed
    /// (e.g. SMS dispatch), and mask (<c>IMobileProtectionService.Mask</c>) everywhere else. The
    /// legacy plaintext <c>Mobile</c> column is left in place (unread by this entity) until a
    /// verified one-time backfill migrates every row's value here — see
    /// <c>m-user-workstream-7-drop-mobile-columns.sql</c> (not yet run).
    /// </summary>
    public string? EncryptedMobile { get; set; }

    public string? Email { get; set; }

    /// <summary>Doubles as this user's display name — <c>M_Users</c> has no dedicated "FullName"
    /// column, and this is the closest legacy field. See <see cref="FullName"/>.</summary>
    public string? ContactPerson { get; set; }

    public string? AlternateEmail { get; set; }

    public string? OTP { get; set; }

    public DateTime? TransactionDate { get; set; }

    public int? OTPAttempt { get; set; }

    /// <summary>Single source of truth for whether this user may log in — true = active, false =
    /// disabled by an administrator. Replaces the legacy Active/UserFreez/UserMFFreez three-flag
    /// model 2026-08-17 (client decision): a user is now disabled for any reason by setting this
    /// one flag to false, rather than juggling three separately-meaning flags.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>True once <see cref="FailedLoginAttempts"/> has reached 5 consecutive failures —
    /// blocks login until an administrator explicitly unlocks the account (distinct from the
    /// existing 15-minute Redis rate-limit, which self-expires; this does not). Added 2026-08-17.</summary>
    public bool IsLocked { get; set; }

    public string? FreezedFunctionIds { get; set; }

    public DateTime? LastEmailChangeDate { get; set; }

    public DateTime? EntryDate { get; set; }

    // -- Compliance-brief fields with no real M_Users column yet (added via a DBA-approved
    // ALTER TABLE, see db-scripts/M_Users_ComplianceColumns.sql — kept from the earlier
    // compliance work rather than silently dropped now that the identity model changed).

    /// <summary>Indicates if user must reset password on next login (also auto-set when a login uses the configured default password, or when the password is older than the configured max age).</summary>
    public bool PasswordResetRequired { get; set; }

    /// <summary>Timestamp when the user's email address was last confirmed/validated (distinct from <see cref="LastEmailChangeDate"/>, which tracks the value changing, not being re-confirmed).</summary>
    public DateTime? EmailLastValidatedAtUtc { get; set; }

    // -- Standard audit columns (present on the real M_Users table).
    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }

    // Navigation properties
    public ICollection<SecurityEvent> SecurityEvents { get; set; } = new List<SecurityEvent>();
    public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public ICollection<MapUserRole> MapUserRoles { get; set; } = new List<MapUserRole>();
    public ICollection<MapUserApp> MapUserApps { get; set; } = new List<MapUserApp>();
    public ICollection<UserFinancialYear> UserFinancialYears { get; set; } = new List<UserFinancialYear>();

    /// <summary>Display name — <c>M_Users</c> has no dedicated column for this, so it falls back
    /// to <see cref="ContactPerson"/>, then <see cref="Username"/>. Not mapped to a column.</summary>
    public string FullName => !string.IsNullOrWhiteSpace(ContactPerson) ? ContactPerson! : Username;
}
