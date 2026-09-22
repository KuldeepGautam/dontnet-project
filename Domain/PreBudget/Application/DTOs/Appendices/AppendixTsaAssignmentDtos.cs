namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixTsaAssignmentDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal? BE { get; set; }
    public decimal? TsaAssignmentAsOnSept { get; set; }
    public decimal? ActualExpenditureUptoSept { get; set; }
    public decimal? UnspentAssignment { get; set; }
    public DateOnly? DateOfLastAssignment { get; set; }
    public decimal? AmountOfLastAssignment { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixTsaAssignmentDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal? BE { get; set; }
    public decimal? TsaAssignmentAsOnSept { get; set; }
    public decimal? ActualExpenditureUptoSept { get; set; }
    public DateOnly? DateOfLastAssignment { get; set; }
    public decimal? AmountOfLastAssignment { get; set; }
}
