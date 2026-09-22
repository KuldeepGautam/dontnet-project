namespace UBIS.Services.UserProfile.Domain.Entities;

/// <summary>
/// Maps to the existing legacy dbo.M_UserIPrequest table (read/write) — confirmed against the
/// DBA's real export (new-tables/M_UserIPrequest.sql, full CREATE TABLE + live data). This table
/// is already used by AIM's own Section-3 self-service change-request flow
/// (Core/AIM/Domain/Entities/Legacy/UserIPRequest.cs) — UserProfile writes to the same real table
/// as a deliberate, user-confirmed choice, not a duplicate/competing table.
/// </summary>
public class UserIpRequest
{
    public int RowId { get; set; }

    /// <summary>Legacy int UserId this request belongs to.</summary>
    public int? UsersId { get; set; }

    public string? DemandId { get; set; }

    /// <summary>Newly-requested primary IP address (legacy column name, misspelled "adres" in the real schema).</summary>
    public string? IPadres1 { get; set; }

    /// <summary>Newly-requested secondary IP address (legacy column name, misspelled "adres" in the real schema).</summary>
    public string? IPadres2 { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    /// <summary>char(1), nullable — see <see cref="ApproveFlagValues"/> for the real observed values.</summary>
    public string? ApproveFlag { get; set; }

    public int? ApproveUserId { get; set; }

    /// <summary>The user's actual system/network IP at the time of the request (not the requested value).</summary>
    public string? IP { get; set; }

    public string? Mobile { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}

/// <summary>
/// Real distinct ApproveFlag values found across the DBA's full M_UserIPrequest export: 'Y', 'N',
/// 'R', and NULL — a char(1) column, not a bit. (AIM's own separate ApproveFlagValues class,
/// Core/AIM/Domain/Entities/Legacy/UserIPRequest.cs, guesses "A" for approved — that never
/// actually appears in the real data, only 'Y' does.) Mapping used here: NULL = Pending,
/// 'Y' = Approved, anything else non-null ('N'/'R') = NotApproved.
/// </summary>
public static class ApproveFlagValues
{
    public const string Approved = "Y";
}
