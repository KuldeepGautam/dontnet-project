namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixCnaSnaBalanceDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Raw SchemeSrNo/SubSchemeSrNo and plain (unprefixed) names - added 2026-09-21 for the
    /// structured Scheme->Sub-Scheme export (client-supplied reference file), same fields/reasoning
    /// as AppendixEstimatesOfSchemesDto (Appendix IV)'s own copies.</summary>
    public int? SchemeSrNo { get; set; }
    public string? SchemeNameRaw { get; set; }
    public string? SubSchemeNameRaw { get; set; }
    public int? SubSchemeSrNo { get; set; }

    public decimal? BE { get; set; }
    public decimal? BalanceAsOnAprilOpening { get; set; }
    public decimal? ReleasesDuringFY { get; set; }
    public decimal? BalanceAsOnSeptClosing { get; set; }
    public DateOnly? DateOfLastRelease { get; set; }
    public decimal? AmountOfLastRelease { get; set; }
    public decimal? NotTransferredToSnaAsOnSept { get; set; }
    public string? Remarks { get; set; }
    public string? ReasonForExemption { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixCnaSnaBalanceDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? BE { get; set; }
    public decimal? BalanceAsOnAprilOpening { get; set; }
    public decimal? ReleasesDuringFY { get; set; }
    public decimal? BalanceAsOnSeptClosing { get; set; }
    [NotFutureDate]
    public DateOnly? DateOfLastRelease { get; set; }
    public decimal? AmountOfLastRelease { get; set; }
    public decimal? NotTransferredToSnaAsOnSept { get; set; }
    public string? Remarks { get; set; }
    public string? ReasonForExemption { get; set; }
}
