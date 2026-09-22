namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixQuarterlyExpenditurePlanDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    public decimal? Q1ApprovedQepPrevYear { get; set; }
    public decimal? Q1ActualsPrevYear { get; set; }
    public decimal? Q1ApprovedQep { get; set; }
    public decimal? Q1Actuals { get; set; }
    public string? RemarksQ1 { get; set; }
    public bool Q1HasDeviation { get; set; }
    public string? Q1MofApprovalDetails { get; set; }

    public decimal? Q2ApprovedQepPrevYear { get; set; }
    public decimal? Q2ActualsPrevYear { get; set; }
    public decimal? Q2ApprovedQep { get; set; }
    public decimal? Q2Actuals { get; set; }
    public string? RemarksQ2 { get; set; }
    public bool Q2HasDeviation { get; set; }
    public string? Q2MofApprovalDetails { get; set; }

    public decimal TotalApprovedQep { get; set; }
    public decimal TotalActuals { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
[RequiredPositiveAmounts(
    "Q1ApprovedQepPrevYear", "Q1ActualsPrevYear", "Q1ApprovedQep", "Q1Actuals",
    "Q2ApprovedQepPrevYear", "Q2ActualsPrevYear", "Q2ApprovedQep", "Q2Actuals",
    ErrorMessage = "Fill Values for Actuals, as per approved qep.")]
public class SaveAppendixQuarterlyExpenditurePlanDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    public decimal? Q1ApprovedQepPrevYear { get; set; }
    public decimal? Q1ActualsPrevYear { get; set; }
    public decimal? Q1ApprovedQep { get; set; }
    public decimal? Q1Actuals { get; set; }
    public string? RemarksQ1 { get; set; }
    public bool Q1HasDeviation { get; set; }
    public string? Q1MofApprovalDetails { get; set; }

    public decimal? Q2ApprovedQepPrevYear { get; set; }
    public decimal? Q2ActualsPrevYear { get; set; }
    public decimal? Q2ApprovedQep { get; set; }
    public decimal? Q2Actuals { get; set; }
    public string? RemarksQ2 { get; set; }
    public bool Q2HasDeviation { get; set; }
    public string? Q2MofApprovalDetails { get; set; }
}
