namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>Read-only reference data mapped onto the shared dbo.M_Scheme table - see MCategory's doc comment.</summary>
public class MScheme
{
    public int SchemeId { get; set; }
    public int DemandId { get; set; }
    public int? CategoryId { get; set; }
    public int? SchemeSrNo { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Prior-year lineage (migrated from legacy BIMSDemo 2026-07-31) - drives the Appendix III "previous year NBE total" reference lookup.</summary>
    public int? PrevSchemeId { get; set; }
}
