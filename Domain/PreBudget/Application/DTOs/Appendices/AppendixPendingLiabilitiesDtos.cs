namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixPendingLiabilitiesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }
    public decimal? PendingLiabilityAsOnMarch31 { get; set; }
    public decimal? BE { get; set; }
    public decimal? EstimatedExpenditure { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixPendingLiabilitiesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? PendingLiabilityAsOnMarch31 { get; set; }
    public decimal? BE { get; set; }
    public decimal? EstimatedExpenditure { get; set; }
    public string? Remarks { get; set; }
}
