namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixEstablishmentByObjectHeadDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ObjectHeadId { get; set; }
    public string? ObjectHeadCode { get; set; }
    public string? ObjectHeadName { get; set; }
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
public class SaveAppendixEstablishmentByObjectHeadDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ObjectHeadId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
}
