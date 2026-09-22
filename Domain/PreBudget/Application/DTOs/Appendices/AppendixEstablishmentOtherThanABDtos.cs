namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixEstablishmentOtherThanABDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixEstablishmentOtherThanABDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
}
