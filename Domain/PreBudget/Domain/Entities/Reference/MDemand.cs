namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the shared dbo.M_Demand table (owned by AIM, not part of
/// PreBudget's own bounded context - PreBudget only ever needed DemandId as a plain int elsewhere,
/// but the Appendix III "previous year NBE total" reference lookup needs PrevDemandId lineage, so
/// this minimal projection was added rather than pulling in the rest of M_Demand's ~40 columns).
/// </summary>
public class MDemand
{
    public int DemandId { get; set; }
    public int? PrevDemandId { get; set; }

    /// <summary>Stable business identifier - unlike DemandId, does not change per financial year (added 2026-08-06 for Autonomous Body lookups).</summary>
    public int DemandNo { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Added 2026-08-28 for the Add Allocation screen's grid ("Demand Name" column,
    /// "{DemandNo} - {DemandName}" format) - vw_Demand already exposes this column, just not
    /// previously mapped since nothing in PreBudget needed it before now.</summary>
    public string DemandName { get; set; } = string.Empty;
}
