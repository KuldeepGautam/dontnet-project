namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

// Appendix V (base): read-only, auto-populated from V-A/V-B/V-C - no entry form.
public class AppendixVSummaryViewModel : AppendixBaseViewModel
{
    public List<AppendixVSummaryRow> Rows { get; set; } = new();
}

public class AppendixVSummaryRow
{
    public string Label { get; set; } = string.Empty;
    public bool IsSectionHeader { get; set; }
    public bool IsTotal { get; set; }
    public decimal? Actual { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? PercentWrtBE { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
}
