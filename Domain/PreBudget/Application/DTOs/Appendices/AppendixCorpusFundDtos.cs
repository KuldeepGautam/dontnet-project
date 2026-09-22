namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixCorpusFundDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public bool IsPublicAccount { get; set; }
    public decimal? AccumulatedBalancePrevYear { get; set; }
    public decimal? AccumulatedBalance { get; set; }
    public decimal? ActualExpenditureY1 { get; set; }
    public decimal? ActualExpenditureY2 { get; set; }
    public decimal? ActualExpenditureY3 { get; set; }
    public decimal? AllocationInBE { get; set; }
    public decimal? ExpenditureTillSept { get; set; }
    public string? ReasonForCorpusFund { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixCorpusFundDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public bool IsPublicAccount { get; set; }
    public decimal? AccumulatedBalancePrevYear { get; set; }
    public decimal? AccumulatedBalance { get; set; }
    public decimal? ActualExpenditureY1 { get; set; }
    public decimal? ActualExpenditureY2 { get; set; }
    public decimal? ActualExpenditureY3 { get; set; }
    public decimal? AllocationInBE { get; set; }
    public decimal? ExpenditureTillSept { get; set; }
    public string? ReasonForCorpusFund { get; set; }
}
