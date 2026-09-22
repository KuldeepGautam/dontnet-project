namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

public class AppendixUserChargesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? TitleOfCharge { get; set; }
    public string? Service { get; set; }
    public string? OrgDept { get; set; }
    public string? RateOfCharge { get; set; }
    public string? UnitOfCollection { get; set; }
    public DateTime? DateOfRateFixation { get; set; }
    public string? FixationStatute { get; set; }
    public decimal? TotalRevenueY1 { get; set; }
    public decimal? TotalRevenueY2 { get; set; }
    public decimal? TotalRevenueY3 { get; set; }
    public string? CompetentAuthority { get; set; }
    public string? PeriodOfFixation { get; set; }
    public decimal? Salary { get; set; }
    public decimal? OfficeExpenses { get; set; }
    public decimal? OtherExpenses { get; set; }
    public bool IsCollectionCostHigher { get; set; }
    public bool IsTransCostHigher { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixUserChargesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    // Max lengths mirror AppendixUserChargesConfiguration's HasMaxLength(250) on each of these
    // columns - without a matching DTO-level check, a too-long value reached the DB unvalidated
    // and raised an unhandled SqlException/500 instead of a clean 400.
    [StringLength(250)]
    public string? TitleOfCharge { get; set; }
    [StringLength(250)]
    public string? Service { get; set; }
    [StringLength(250)]
    public string? OrgDept { get; set; }
    [StringLength(250)]
    public string? RateOfCharge { get; set; }
    [StringLength(250)]
    public string? UnitOfCollection { get; set; }
    public DateTime? DateOfRateFixation { get; set; }
    [StringLength(250)]
    public string? FixationStatute { get; set; }
    public decimal? TotalRevenueY1 { get; set; }
    public decimal? TotalRevenueY2 { get; set; }
    public decimal? TotalRevenueY3 { get; set; }
    [StringLength(250)]
    public string? CompetentAuthority { get; set; }
    [StringLength(250)]
    public string? PeriodOfFixation { get; set; }
    public decimal? Salary { get; set; }
    public decimal? OfficeExpenses { get; set; }
    public decimal? OtherExpenses { get; set; }
    public bool IsCollectionCostHigher { get; set; }
    public bool IsTransCostHigher { get; set; }
    public string? Remarks { get; set; }
}
