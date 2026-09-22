namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixLoansToGovtServantsDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SubHeadName { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY3 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixLoansToGovtServantsDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SubHeadName { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY3 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
}
