namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix IV-B: Estimates under Tribal Area Sub Plan (Minor Head 796). Maps to dbo.AppendixTaspExpenditure (was legacy Temp_IVB_STSubPlan). Same shape as Appendix IV-A.</summary>
public class AppendixTaspExpenditure : AppendixEntityBase
{
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? AddlReSought { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? AddlNbeSought { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>FRS: Additional RE/NBE Sought are system-calculated, read-only fields (BR-21/BR-25 pattern reused across the Appendix IV family).</summary>
    public void UpdateFrom(
        int? schemeId,
        int? subSchemeId,
        decimal? actuals,
        decimal? actualsUptoSeptPrevYear,
        decimal? be,
        decimal? actualsUptoSept,
        decimal? proposedRE,
        decimal? proposedNBE,
        string? remarksMinistry,
        string? remarksBudget,
        decimal? budgetRecommendedRE,
        decimal? budgetRecommendedNBE)
    {
        SchemeId = schemeId;
        SubSchemeId = subSchemeId;
        Actuals = actuals;
        ActualsUptoSeptPrevYear = actualsUptoSeptPrevYear;
        BE = be;
        ActualsUptoSept = actualsUptoSept;
        ProposedRE = proposedRE;
        ProposedNBE = proposedNBE;
        RemarksMinistry = remarksMinistry;
        RemarksBudget = remarksBudget;
        // Client instruction 2026-09-21: Budget Division/ABO-DS-Director/Section User enter these
        // two directly now (edit-drawer fields, gated to those roles).
        BudgetRecommendedRE = budgetRecommendedRE;
        BudgetRecommendedNBE = budgetRecommendedNBE;
        AddlReSought = (proposedRE ?? 0) - (be ?? 0);
        AddlNbeSought = (proposedNBE ?? 0) - (be ?? 0);
    }
}
