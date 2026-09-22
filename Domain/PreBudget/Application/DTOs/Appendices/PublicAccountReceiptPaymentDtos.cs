namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class PublicAccountReceiptPaymentDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? MajorHeadId { get; set; }
    public decimal? ActualReceipt { get; set; }
    public decimal? ActualPayment { get; set; }
    public decimal? BalanceAtEndReceipt { get; set; }
    public decimal? BalanceAtEndPayment { get; set; }
    public decimal? BEReceipt { get; set; }
    public decimal? BEPayment { get; set; }
    public decimal? AdjustmentReceipt { get; set; }
    public decimal? AdjustmentPayment { get; set; }
    public decimal? REReceipt { get; set; }
    public decimal? REPayment { get; set; }
    public decimal? NBEReceipt { get; set; }
    public decimal? NBEPayment { get; set; }
    public string? RemarksReceipt { get; set; }
    public string? RemarksPayment { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SavePublicAccountReceiptPaymentDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? MajorHeadId { get; set; }
    public decimal? ActualReceipt { get; set; }
    public decimal? ActualPayment { get; set; }
    public decimal? BalanceAtEndReceipt { get; set; }
    public decimal? BalanceAtEndPayment { get; set; }
    public decimal? BEReceipt { get; set; }
    public decimal? BEPayment { get; set; }
    public decimal? AdjustmentReceipt { get; set; }
    public decimal? AdjustmentPayment { get; set; }
    public decimal? REReceipt { get; set; }
    public decimal? REPayment { get; set; }
    public decimal? NBEReceipt { get; set; }
    public decimal? NBEPayment { get; set; }
    public string? RemarksReceipt { get; set; }
    public string? RemarksPayment { get; set; }
}
