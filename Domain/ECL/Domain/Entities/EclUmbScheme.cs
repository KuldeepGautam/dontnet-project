namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only reference entity mapping the new dbo.M_UmbScheme table (added 2026-08-19,
/// structure matched to the real BIMSDemo.dbo.M_UmbScheme, data copied across from there) — the
/// real source of Umbrella Scheme picker options for ECL Master's "Add Schemes" screen, filtered by
/// CategoryId AND DemandId. Previously this picker wrongly reused dbo.M_Scheme.IsUmbrella, which
/// isn't the real umbrella-scheme master table at all.</summary>
public class EclUmbScheme
{
    public int UmbSchemeId { get; set; }

    public int? DemandId { get; set; }

    public int? CategoryId { get; set; }

    public int? SubCategoryId { get; set; }

    public string? UmSchemeName { get; set; }

    public string? HUmSchemeName { get; set; }

    public string? UmbSchemeType { get; set; }

    /// <summary>"Y"/"N" (matches dbo.M_Scheme.Active's char convention, not this repo's usual bit IsActive).</summary>
    public string? Active { get; set; }

    public DateTime? EntryDate { get; set; }

    public int? PrevUmbSchemeId { get; set; }

    public int? DisplaySeqNo { get; set; }

    public string? Ip { get; set; }

    public int? UserId { get; set; }
}
