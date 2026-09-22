namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixRecoveriesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SchemeName { get; set; }
    public int? MajorHeadId { get; set; }
    public string? MajorHeadName { get; set; }
    public bool IsCharged { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixRecoveriesDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SchemeName { get; set; }
    public int? MajorHeadId { get; set; }
    public bool IsCharged { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
}
