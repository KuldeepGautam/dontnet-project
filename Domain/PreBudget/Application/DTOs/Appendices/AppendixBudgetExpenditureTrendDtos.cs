namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixBudgetExpenditureTrendDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixBudgetExpenditureTrendDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }
}
