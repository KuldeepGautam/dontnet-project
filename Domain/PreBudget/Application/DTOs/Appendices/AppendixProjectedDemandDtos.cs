namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixProjectedDemandDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? PrevYrMinRevenueBE { get; set; }
    public decimal? PrevYrMinRevenueRE { get; set; }
    public decimal? PrevYrMinCapitalBE { get; set; }
    public decimal? PrevYrMinCapitalRE { get; set; }
    public decimal? CurrYrMinRevenueBE { get; set; }
    public decimal? CurrYrMinCapitalBE { get; set; }
    public decimal? CurrYrMinMtefRevenueBE { get; set; }
    public decimal? CurrYrMinMtefCapitalBE { get; set; }
    public decimal TotalBE { get; set; }
    public decimal PrevYrTotalBE { get; set; }
    public decimal PrevYrTotalRE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixProjectedDemandDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? PrevYrMinRevenueBE { get; set; }
    public decimal? PrevYrMinRevenueRE { get; set; }
    public decimal? PrevYrMinCapitalBE { get; set; }
    public decimal? PrevYrMinCapitalRE { get; set; }
    public decimal? CurrYrMinRevenueBE { get; set; }
    public decimal? CurrYrMinCapitalBE { get; set; }
    public decimal? CurrYrMinMtefRevenueBE { get; set; }
    public decimal? CurrYrMinMtefCapitalBE { get; set; }
}
