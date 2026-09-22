namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix V-C: Establishment Expenditure - Other than AB. Maps to dbo.AppendixEstablishmentOtherThanAB (was legacy Temp_EstExp_OtherthanAB).</summary>
public class AppendixEstablishmentOtherThanAB : AppendixEntityBase
{
    public string Name { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        string name,
        decimal? actuals,
        decimal? actualsUptoSeptPrevYear,
        decimal? be,
        decimal? actualsUptoSept,
        decimal? proposedRE,
        decimal? proposedNBE,
        string? remarks)
    {
        Name = name;
        Actuals = actuals;
        ActualsUptoSeptPrevYear = actualsUptoSeptPrevYear;
        BE = be;
        ActualsUptoSept = actualsUptoSept;
        ProposedRE = proposedRE;
        ProposedNBE = proposedNBE;
        Remarks = remarks;
    }
}
