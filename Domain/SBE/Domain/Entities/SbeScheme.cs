namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>Maps dbo.M_Scheme (shared with ECL/PreBudget). Foundation-stage: plain data holder for reads/joins/display formatting only.</summary>
public class SbeScheme
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
    public int? UmbSchemeId { get; set; }
}
