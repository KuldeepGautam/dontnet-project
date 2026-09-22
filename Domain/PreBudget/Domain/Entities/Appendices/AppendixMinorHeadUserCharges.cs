namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-F: User Charges of Ministries/Departments and its various organizations,
/// Receipts collected in Consolidated Fund of India - keyed by Minor Head (Major+Minor Head
/// composite code, e.g. "240100103") rather than Scheme (VI-A) or Autonomous Body (VI-G). Maps to
/// dbo.Appendix_VIF_MinorHeadUserCharges. New appendix, added 2026-09-09 per
/// UBIS-Pre-budget-html/Appendix6f.html.</summary>
public class AppendixMinorHeadUserCharges : AppendixEntityBase
{
    /// <summary>Major+Minor Head composite code (9 chars, e.g. "240100103") - see MinorHeadName's
    /// own doc comment for how this is looked up/named.</summary>
    public string MinorHeadCode { get; set; } = string.Empty;
    public string? BriefOnReceipts { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? NoOfTransactions { get; set; }
    public string? RateOfService { get; set; }
    public decimal? ReceiptsCollection { get; set; }
    public string? ActionTakenPlan { get; set; }

    public void UpdateFrom(
        string minorHeadCode,
        string? briefOnReceipts,
        string? presentStatus,
        decimal? noOfTransactions,
        string? rateOfService,
        decimal? receiptsCollection,
        string? actionTakenPlan)
    {
        MinorHeadCode = minorHeadCode;
        BriefOnReceipts = briefOnReceipts;
        PresentStatus = presentStatus;
        NoOfTransactions = noOfTransactions;
        RateOfService = rateOfService;
        ReceiptsCollection = receiptsCollection;
        ActionTakenPlan = actionTakenPlan;
    }
}
