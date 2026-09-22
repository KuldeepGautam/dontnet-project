namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix IV: Estimates of Schemes. Maps to dbo.AppendixEstimatesOfSchemes (was legacy Temp_EstimatesofSchemes). SchemeId/SubSchemeId are external ReferenceData ids.</summary>
public class AppendixEstimatesOfSchemes : AppendixEntityBase
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

    /// <summary>Auto-calculated, read-only per FRS: % w.r.t. B.E.</summary>
    public decimal? PercentWrtBE => BE is > 0 ? Math.Round((ActualsUptoSept ?? 0) / BE.Value * 100, 2) : null;

    /// <summary>
    /// Additional RE/NBE Sought are system-calculated, read-only fields (BR-21/BR-25 pattern reused
    /// across the Appendix IV family - see AppendixScspExpenditure/AppendixTaspExpenditure).
    /// BudgetRecommendedRE/NBE are plain user-entered fields, gated to Budget-side roles only on the
    /// UBIS_Web edit drawer (client instruction 2026-09-21) - unlike AddlReSought/AddlNbeSought,
    /// nothing here derives them.
    /// </summary>
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
        // two directly now (edit-drawer fields, gated to those roles) - no longer untouched/always
        // null as this entity's own earlier doc comment described.
        BudgetRecommendedRE = budgetRecommendedRE;
        BudgetRecommendedNBE = budgetRecommendedNBE;
        AddlReSought = (proposedRE ?? 0) - (be ?? 0);
        AddlNbeSought = (proposedNBE ?? 0) - (be ?? 0);
    }
}
