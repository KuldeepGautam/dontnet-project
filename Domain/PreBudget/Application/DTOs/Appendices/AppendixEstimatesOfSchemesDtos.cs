namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixEstimatesOfSchemesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Scheme's own M_Category.CategoryName (via M_Scheme.CategoryId) - added 2026-09-18
    /// so the export can group rows into the government circular's "Centrally Sponsored Schemes" /
    /// "Central Sector Schemes" sections, same reference data Appendix III/III-B already resolve.
    /// Null when the Scheme has no CategoryId set.</summary>
    public string? CategoryType { get; set; }

    /// <summary>M_Category.SerialNo ("I".."VI") for the Scheme's CategoryType above - added
    /// 2026-09-21 for the Budget Division export's Category ordering (client instruction: "use
    /// category sr no to group"), same column/string-sort convention as
    /// AppendixIIIBController.GetCategories' own `.OrderBy(c => c.SerialNo)`.</summary>
    public string? CategorySerialNo { get; set; }

    /// <summary>Raw M_Scheme.SchemeSrNo (not the formatted "N - Name" display string) - added
    /// 2026-09-21 so the Budget Division export can order/group by Scheme without re-parsing
    /// SchemeName.</summary>
    public int? SchemeSrNo { get; set; }

    /// <summary>Plain M_Scheme.SchemeName with no "N - " prefix - added 2026-09-21. The Budget
    /// Division export's row labels use its own "N. Name"/"N.NN- Name" punctuation (client-supplied
    /// reference file), distinct from SchemeDisplayFormatter/SubSchemeDisplayFormatter's shared
    /// "N - Name" grid/dropdown convention used everywhere else - keeping that shared formatter
    /// untouched, this export builds its own label from the raw name instead.</summary>
    public string? SchemeNameRaw { get; set; }

    /// <summary>Plain M_SubScheme.SubSchemeName with no prefix, and its raw SubSchemeSrNo - same
    /// reason as SchemeNameRaw above.</summary>
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
    public decimal? PercentWrtBE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixEstimatesOfSchemesDto
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
    /// UBIS_Web edit drawer (client instruction 2026-09-21) - null/unchanged passthrough on a save
    /// from any other role's copy of the form.</summary>
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
}
