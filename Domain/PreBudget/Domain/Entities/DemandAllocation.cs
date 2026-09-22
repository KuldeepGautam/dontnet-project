namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to dbo.M_DemandAppendixAllocation — renamed 2026-09-08 (client correction) from
/// dbo.M_PB_DemandAppendixAllocation (the "PB_" infix from that same day's earlier rename was not
/// what was asked for), which itself had renamed from dbo.M_DemandAllocation, which itself replaced
/// legacy Temp_DemandAllocation (renamed 2026-08-28, client instruction, from
/// dbo.PreBudgetSubmissionStatus which had held this exact same data/shape since its own
/// introduction). All renames are straight table renames, no data/shape change - see
/// Others/publish-staging/prebudget-workstream-rename-pb-demandappendixallocation-to-demandappendixallocation.sql
/// (plus its compat-synonym companion, same pattern as every prior rename in this chain),
/// prebudget-workstream-rename-demandallocation-to-pb-demandappendixallocation.sql, and
/// prebudget-workstream-rename-submissionstatus-to-demandallocation.sql for the full migration
/// history. One row per (Demand, Appendix, FinancialYear); drives the Select-Demand-and-Appendix
/// screen (old sp_Select_Demands_Template) and the Allocation screen. Client review 2026-08-05:
/// dropped the stored Status column (legacy Temp_DemandAllocation never had one either) — frozen/
/// nil/expired state is derived from FrozenAtUtc/NilRemarks/TargetDate directly instead of a
/// separately-tracked Freez/NilData char(1) flag that could drift out of sync with them; that
/// improvement was kept across every rename (rename only, no revert to the old flag shape).
/// </summary>
public class DemandAllocation
{
    public int Id { get; set; }

    public int DemandId { get; set; }

    public int AppendixId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public DateTime? TargetDate { get; set; }

    public string? NilRemarks { get; set; }

    public DateTime? FrozenAtUtc { get; set; }

    public int? FrozenByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
