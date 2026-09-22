namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the shared dbo.M_Category table (DBA-owned, not part of
/// PreBudget's own bounded context - migrated 2026-07-29 from the legacy BIMSDemo database
/// alongside M_Scheme/M_SubScheme so Appendix III's Balance Type -> Scheme -> SubScheme cascade
/// can be driven by real reference data instead of free-text/number inputs).
/// </summary>
public class MCategory
{
    public int CategoryId { get; set; }
    public string? FinancialYear { get; set; }
    public string? SerialNo { get; set; }
    public string? CategoryName { get; set; }
    public string? HCategoryName { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Prior-year lineage - drives the Appendix III "previous year NBE total" reference lookup.</summary>
    public int? PrevCategoryId { get; set; }
}
