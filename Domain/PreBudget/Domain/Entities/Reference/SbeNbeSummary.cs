namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto dbo.SbeNbeSummary - a local pre-aggregation of legacy
/// BIMSDemo.dbo.SBEData.NBE_PLan (grouped by DemandId/CategoryId/SchemeId/SubSchemeId, no
/// FinancialYear - matches legacy stored procedure Previous_Dem_Sch_Cat_SubSchId, which always
/// consumes NBE_PLan as a SUM across all years for that key). SBEData itself belongs to a different,
/// much larger legacy module (Statement of Budget Estimates, ~45k rows/39 columns) outside
/// PreBudget's bounded context, so only the single aggregate this proc actually needs was migrated
/// - not the raw table (migrated 2026-07-31).
/// </summary>
public class SbeNbeSummary
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public int CategoryId { get; set; }
    public int SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal NbeTotal { get; set; }
}
