namespace UBIS.Services.PreBudget.Application.DTOs;

public class RemarkDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RemarkText { get; set; } = string.Empty;
    public string? CreatedByRoleSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateRemarkDto
{
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RemarkText { get; set; } = string.Empty;
}
