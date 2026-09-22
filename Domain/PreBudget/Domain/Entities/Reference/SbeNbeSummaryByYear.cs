namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto dbo.SbeNbeSummaryByYear - a year-scoped pre-aggregation of
/// legacy BIMSDemo.dbo.SBEData.NBE_PLan (grouped by DemandId/FinancialYear/SchemeId/SubSchemeId).
/// Separate from SbeNbeSummary (which is deliberately NOT year-scoped, matching the legacy
/// Previous_Dem_Sch_Cat_SubSchId proc) - this one backs the "BE auto-loaded from SBEData for the
/// current FY" requirement on Appendix III/IV/IV-A/IV-B (client review 2026-08-05).
/// </summary>
public class SbeNbeSummaryByYear
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal NbeTotal { get; set; }
}
