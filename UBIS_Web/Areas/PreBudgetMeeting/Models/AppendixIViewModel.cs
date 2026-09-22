namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>Appendix I: Budget and Expenditure Trends.</summary>
public class AppendixIViewModel : AppendixBaseViewModel
{
    /// <summary>All saved records for this Demand within the 5-year window (ActualYear-4..ActualYear
    /// - see ActualYear below) - shown in the drawer/grid theme's data grid (2026-09-09 redesign,
    /// replacing both the old inline 5-year matrix form and the "Hide grid from appendix 1"
    /// read-only table it was hidden behind since 2026-09-02).</summary>
    public List<AppendixBudgetExpenditureTrendDto> Records { get; set; } = new();

    /// <summary>The Add/Edit drawer's bound record. FinancialYear is always forced server-side to
    /// ActualYear (see below) - never trusted from client input, since ActualYear is the only year
    /// this screen ever permits creating/editing a row for.</summary>
    public SaveAppendixBudgetExpenditureTrendDto NewRecord { get; set; } = new();

    /// <summary>Client review 2026-08-06 ("Actual Year" terminology): the one year this screen ever
    /// permits Add/Edit on = current session FinancialYear - 2 (e.g. session FY 2026-2027 ->
    /// ActualYear 2024-2025). Every other year in the 5-year Records window is settled historical
    /// fact, shown read-only with no Edit/Delete action. Drives the header Add button's enabled
    /// state (2026-09-09 requirement: "add button is enabled only if current year data is not
    /// available") - computed once here so the view and controller share the same value instead of
    /// each re-deriving it (and risking drift, as the old dual IsCurrentYear-condition below did).</summary>
    public string ActualYear { get; set; } = string.Empty;

    /// <summary>Retained for FreezeAppendixIBulk (dead code - no current view posts to it, since the
    /// header's Freeze Data button already uses the generic FreezeAppendixTemplate action like every
    /// other appendix) rather than risk breaking it by removing Rows outright.</summary>
    public List<AppendixIYearRowViewModel> Rows { get; set; } = new();
}

public class AppendixIYearRowViewModel
{
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Null until this year's row has been saved once - Create vs Update is decided by
    /// whether this is present when the grid is submitted.</summary>
    public int? Id { get; set; }

    public bool IsFrozen { get; set; }

    /// <summary>Only this year's row is editable in the grid (2026-08-04) - every other year shows
    /// its saved values read-only, since past years are settled fact, not something to re-enter.</summary>
    public bool IsCurrentYear { get; set; }

    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }
}
