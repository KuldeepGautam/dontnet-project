namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix I: Budget and Expenditure Trends. Maps to dbo.AppendixBudgetExpenditureTrend (was legacy Temp_BudExpTrends).</summary>
public class AppendixBudgetExpenditureTrend : AppendixEntityBase
{
    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }

    public void UpdateFrom(
        decimal? revenueBE,
        decimal? revenueRE,
        decimal? revenueActuals,
        decimal? revenueActualsUptoSept,
        decimal? capitalBE,
        decimal? capitalRE,
        decimal? capitalActuals,
        decimal? capitalActualsUptoSept)
    {
        RevenueBE = revenueBE;
        RevenueRE = revenueRE;
        RevenueActuals = revenueActuals;
        RevenueActualsUptoSept = revenueActualsUptoSept;
        CapitalBE = capitalBE;
        CapitalRE = capitalRE;
        CapitalActuals = capitalActuals;
        CapitalActualsUptoSept = capitalActualsUptoSept;
    }
}
