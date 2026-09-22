namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only reference entity mapping dbo.M_MapDemandFY — same DemandNo-resolution pattern
/// as PreBudget's MDemand (Domain/PreBudget/Domain/Entities/Reference/MDemand.cs). DemandNo is the
/// stable identifier across financial years, unlike DemandId which is minted fresh each year.</summary>
public class EclDemand
{
    public int DemandId { get; set; }

    public int DemandNo { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Added 2026-08-19 for grid display ("DemandNo - DemandName") — was previously
    /// unmapped since this entity only needed DemandNo resolution before.</summary>
    public string? DemandName { get; set; }
}
