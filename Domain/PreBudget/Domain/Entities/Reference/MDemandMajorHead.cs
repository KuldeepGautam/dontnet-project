namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the new dbo.M_DemandMajorHead table (migrated 2026-08-05
/// from legacy BIMSDemo.dbo.M_DemandMajorHead) - drives Appendix VII-A's Major Head dropdown, scoped
/// to the selected Demand: "select majorheadname from M_DemandMajorHead mh inner join M_MajorHead mj
/// on mh.MajorHeadCode = mj.MajorHeadCode where mh.DemandId = @demandid" (client review 2026-08-05).
/// </summary>
public class MDemandMajorHead
{
    public int DemandMajHeadId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int DemandId { get; set; }
    public int? DemandNo { get; set; }
    public string MajorHeadCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
