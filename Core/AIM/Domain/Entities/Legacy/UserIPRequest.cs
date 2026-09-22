namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read/write pass-through to the pre-existing legacy <c>dbo.M_UserIPrequest</c> table — the
/// approval log for hardware (IP) rebinding requests, and (per this compliance pass) also used
/// for pending Email/Mobile self-service change requests submitted from the Profile screen.
/// Table name corrected 2026-07-13 (was missing the "M_" prefix) against the DBA's
/// <c>new-tables/M_UserIPrequest.sql</c> export — same table referenced in the compliance brief.
/// Added 2026-07-10.
/// </summary>
public class UserIPRequest
{
    public int RowId { get; set; }

    /// <summary>Legacy int UserId (see <see cref="LegacyUserRecord.UserId"/>) the request belongs to.</summary>
    public int UsersId { get; set; }

    public int? DemandId { get; set; }

    public string? IPadres1 { get; set; }

    public string? IPadres2 { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    /// <summary>Legacy convention (not fully enumerated in any available reference): observed value "R". Treated here as pending/rejected/approved via <see cref="ApproveFlagValues"/>.</summary>
    public string? ApproveFlag { get; set; }

    public int? ApproveUserId { get; set; }

    public string? IP { get; set; }

    public string? Mobile { get; set; }
}

/// <summary>
/// Candidate <see cref="UserIPRequest.ApproveFlag"/> values used by this application's own new
/// workflow (Section 3 approval + kill-switch). Not a fully-grounded legacy enumeration — see
/// COMPLIANCE_NOTES.md. Chosen to be additive: existing legacy rows using "R" are left as-is.
/// </summary>
public static class ApproveFlagValues
{
    public const string Pending = "P";
    public const string Approved = "A";
    public const string Rejected = "R";
}
