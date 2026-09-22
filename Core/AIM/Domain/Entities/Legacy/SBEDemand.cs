namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read-only pass-through to the pre-existing legacy <c>dbo.M_SBEDemand</c> table — one row
/// per Demand per financial year, carrying its freeze/edit-control state. A Statement Owner
/// (see <see cref="StmtControl"/>) inherits access to every row here sharing the same
/// <see cref="FinancialYear"/> (the only column both tables have in common — see
/// COMPLIANCE_NOTES.md for why a broader join wasn't available). Added 2026-07-10.
/// </summary>
public class SBEDemand
{
    public int SBEDemandId { get; set; }

    public string? FinancialYear { get; set; }

    public int DemandId { get; set; }

    public int? UserId { get; set; }

    public string? Status { get; set; }

    public DateTime? EntryDate { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime? UnFreezedDate { get; set; }

    public string? Freez { get; set; }

    public DateTime? SBEFreezDate { get; set; }

    public int? FreezUserId { get; set; }

    public int? UnFreezeUserId { get; set; }

    public string? Actual_Edit_Flag { get; set; }

    public string? BE_Edit_Flag { get; set; }

    public string? Actual_edit_flag_ddg { get; set; }

    public DateTime? Edit_target_date_ddg { get; set; }

    public string? Scheme_Cat_Edit_Flag { get; set; }

    public string? Allow_NegExp_Flag { get; set; }

    public string? Allow_NegExp_Flag_ddg { get; set; }

    public string? Allow_NegRec_Flag { get; set; }

    public DateTime? Edit_TargetDate { get; set; }

    public DateTime? DDGTargetDate { get; set; }

    public string? DDGFreez { get; set; }

    public DateTime? DDGFreezDate { get; set; }

    public string? ConsumedStatus { get; set; }

    public DateTime? DDGUnFreezedDate { get; set; }

    public int? DDGFreezUserId { get; set; }

    public int? DDGUnFreezeUserId { get; set; }
}
