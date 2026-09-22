namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>Maps dbo.M_UmbScheme (shared with ECL). Foundation-stage: plain data holder; FR024 Masters-Add-Umbrella-Scheme write behavior is added in Stage 2.</summary>
public class SbeUmbScheme
{
    public int UmbSchemeId { get; set; }
    public int? DemandId { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public string? UmSchemeName { get; set; }
    public string? HUmSchemeName { get; set; }
    public string? UmbSchemeType { get; set; }
    public string? Active { get; set; }
    public DateTime? EntryDate { get; set; }
    public int? PrevUmbSchemeId { get; set; }
    public int? DisplaySeqNo { get; set; }
}
