namespace UBIS.Services.ReferenceData.Domain.Entities;

/// <summary>Maps to the new dbo.M_SubScheme table. Mirrors legacy BIMSDemo.M_SubScheme.</summary>
public class SubScheme
{
    public int SubSchemeId { get; set; }

    public int SchemeId { get; set; }

    public string SubSchemeName { get; set; } = string.Empty;

    public string? HSubSchemeName { get; set; }

    public string? SubSchemeCode { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
