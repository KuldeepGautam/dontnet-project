namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

public class AppendixMinorHeadUserChargesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string MinorHeadCode { get; set; } = string.Empty;
    /// <summary>Resolved via MinorHeadName (dbo.M_MinorHeadName) for this FinancialYear - null when
    /// the legacy master has no name recorded for this code/year combination.</summary>
    public string? MinorHeadName { get; set; }
    public string? BriefOnReceipts { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? NoOfTransactions { get; set; }
    public string? RateOfService { get; set; }
    public decimal? ReceiptsCollection { get; set; }
    public string? ActionTakenPlan { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixMinorHeadUserChargesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    [Required]
    [StringLength(9, MinimumLength = 4)]
    public string MinorHeadCode { get; set; } = string.Empty;
    [StringLength(500)]
    public string? BriefOnReceipts { get; set; }
    [StringLength(500)]
    public string? PresentStatus { get; set; }
    public decimal? NoOfTransactions { get; set; }
    [StringLength(250)]
    public string? RateOfService { get; set; }
    public decimal? ReceiptsCollection { get; set; }
    [StringLength(500)]
    public string? ActionTakenPlan { get; set; }
}

/// <summary>One entry in the Minor Head autocomplete list - see AppendixVIFController.SearchMinorHeads.</summary>
public class MinorHeadSuggestionDto
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>Response for AppendixVIFController.GetMinorHeadName.</summary>
public class MinorHeadNameDto
{
    public bool Found { get; set; }
    public string? Name { get; set; }
}
