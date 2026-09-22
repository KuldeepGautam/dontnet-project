namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix I-A: Projected Demand by Ministry. Maps to dbo.AppendixProjectedDemand (was legacy Temp_ProjDemand).</summary>
public class AppendixProjectedDemand : AppendixEntityBase
{
    public decimal? PrevYrMinRevenueBE { get; set; }
    public decimal? PrevYrMinRevenueRE { get; set; }
    public decimal? PrevYrMinCapitalBE { get; set; }
    public decimal? PrevYrMinCapitalRE { get; set; }
    public decimal? CurrYrMinRevenueBE { get; set; }
    public decimal? CurrYrMinCapitalBE { get; set; }
    public decimal? CurrYrMinMtefRevenueBE { get; set; }
    public decimal? CurrYrMinMtefCapitalBE { get; set; }

    /// <summary>Auto-calculated, read-only per FRS: Revenue BE + Capital BE for the proposed year.</summary>
    public decimal TotalBE => (CurrYrMinRevenueBE ?? 0) + (CurrYrMinCapitalBE ?? 0);

    /// <summary>Auto-calculated, read-only: prior-year Revenue BE + Capital BE (client issue
    /// 2026-08-27 - totals for the prior-year row weren't computing automatically on page load).</summary>
    public decimal PrevYrTotalBE => (PrevYrMinRevenueBE ?? 0) + (PrevYrMinCapitalBE ?? 0);

    /// <summary>Auto-calculated, read-only: prior-year Revenue RE + Capital RE.</summary>
    public decimal PrevYrTotalRE => (PrevYrMinRevenueRE ?? 0) + (PrevYrMinCapitalRE ?? 0);

    public void UpdateFrom(
        decimal? prevYrMinRevenueBE,
        decimal? prevYrMinRevenueRE,
        decimal? prevYrMinCapitalBE,
        decimal? prevYrMinCapitalRE,
        decimal? currYrMinRevenueBE,
        decimal? currYrMinCapitalBE,
        decimal? currYrMinMtefRevenueBE,
        decimal? currYrMinMtefCapitalBE)
    {
        PrevYrMinRevenueBE = prevYrMinRevenueBE;
        PrevYrMinRevenueRE = prevYrMinRevenueRE;
        PrevYrMinCapitalBE = prevYrMinCapitalBE;
        PrevYrMinCapitalRE = prevYrMinCapitalRE;
        CurrYrMinRevenueBE = currYrMinRevenueBE;
        CurrYrMinCapitalBE = currYrMinCapitalBE;
        CurrYrMinMtefRevenueBE = currYrMinMtefRevenueBE;
        CurrYrMinMtefCapitalBE = currYrMinMtefCapitalBE;
    }
}
