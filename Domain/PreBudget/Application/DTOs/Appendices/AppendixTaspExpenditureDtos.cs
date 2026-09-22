namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixTaspExpenditureDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Raw SchemeSrNo/SubSchemeSrNo and plain (unprefixed) names - added 2026-09-21 for the
    /// structured Scheme->Sub-Scheme export, same fields/reasoning as AppendixEstimatesOfSchemesDto
    /// (Appendix IV)'s own copies.</summary>
    public int? SchemeSrNo { get; set; }
    public string? SchemeNameRaw { get; set; }
    public string? SubSchemeNameRaw { get; set; }
    public int? SubSchemeSrNo { get; set; }

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
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixTaspExpenditureDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>Budget Division/ABO-DS-Director/Section User-entered, gated to those roles on the
    /// UBIS_Web edit drawer (client instruction 2026-09-21).</summary>
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
}
