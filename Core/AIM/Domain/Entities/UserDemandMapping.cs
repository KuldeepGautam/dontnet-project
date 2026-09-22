namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_MapUserDemand</c> table (int identity, renamed 2026-08-04
/// from M_UserDemandMapping) — records which Demand IDs a user can access. The single-default-
/// Demand <c>MapUserRole.DemandCode</c> concept this used to be described relative to was removed
/// 2026-08-04 (it was 99.9% NULL and unused downstream) - this table is now the sole source of
/// Demand access. Added 2026-07-13 per user-confirmed decision to surface Demand access as
/// JWT claims (<c>AIM:Demand:{demandId}</c>), same existence-based pattern as Function claims.
/// <see cref="PrevRowId"/> chains a person's mapping row forward across re-assignments, same
/// pattern as <see cref="User.PrevUserId"/>. No FK constraint to <c>M_Users</c> exists on the real
/// table (unlike <see cref="MapUserRole"/>), so none is declared here either.
/// </summary>
public class UserDemandMapping
{
    public int RowId { get; set; }

    public int UserId { get; set; }

    /// <summary>Comma-separated list of Demand IDs (legacy column, e.g. "104", or "12,45,90") —
    /// column name is singular ("DemandId") despite holding a list (the same CSV-in-a-column
    /// convention the old <c>User.AppId</c> column used before it was normalized into
    /// <see cref="MapUserApp"/> on 2026-08-03).</summary>
    public string? DemandIds { get; set; }

    public int? PrevRowId { get; set; }

    public bool? IsActive { get; set; } = true;

    // Standard audit columns.
    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }
}
