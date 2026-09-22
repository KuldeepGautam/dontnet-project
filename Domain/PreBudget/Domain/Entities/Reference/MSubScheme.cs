namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>Read-only reference data mapped onto the shared dbo.M_SubScheme table - see MCategory's doc comment.</summary>
public class MSubScheme
{
    public int SubSchemeId { get; set; }
    public int SchemeId { get; set; }
    public string SubSchemeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Prior-year lineage (migrated from legacy BIMSDemo 2026-07-31) - drives the Appendix III "previous year NBE total" reference lookup.</summary>
    public int? PrevSubschemeId { get; set; }

    /// <summary>Legacy Sub-Scheme Sr. No. (backfilled from BIMSDemo 2026-08-04), same convention as
    /// <c>MScheme.SchemeSrNo</c> - dropdown labels are ISNULL(SubSchemeSrNo,0)-SubSchemeName.</summary>
    public int? SubSchemeSrNo { get; set; }
}
