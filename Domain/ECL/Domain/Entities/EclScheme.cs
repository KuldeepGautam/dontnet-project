namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only reference entity mapping dbo.M_Scheme.</summary>
public class EclScheme
{
    public int SchemeId { get; set; }

    public int DemandId { get; set; }

    public string SchemeName { get; set; } = string.Empty;

    public string? HSchemeName { get; set; }

    public bool IsUmbrella { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int? CategoryId { get; set; }

    public int? SchemeSrNo { get; set; }

    public int? PrevSchemeId { get; set; }

    /// <summary>FK to dbo.M_UmbScheme.UmbSchemeID (added 2026-08-19) — a scheme can, but doesn't
    /// have to, belong to an umbrella scheme. NULL means it doesn't.</summary>
    public int? UmbSchemeId { get; set; }

    /// <summary>Client IP of the user who created this scheme (added 2026-08-19), same pattern as
    /// ECL_T_Outlay.IPDemand.</summary>
    public string? Ip { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }
}
