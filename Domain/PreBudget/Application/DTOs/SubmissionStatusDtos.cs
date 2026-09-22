namespace UBIS.Services.PreBudget.Application.DTOs;

public class SetNilSubmissionDto
{
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? NilRemarks { get; set; }
}
