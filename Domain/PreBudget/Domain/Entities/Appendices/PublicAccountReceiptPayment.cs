namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>
/// Public Account Template: RE/BE for inclusion in Budget Estimates (Article 266(2)). Maps to
/// dbo.PublicAccountReceiptPayment (was legacy Temp_PA_RecPay). Field list is explicitly
/// provisional per the FRS itself (Open Item OI-05) — needs Budget Division sign-off before this
/// screen ships, unlike the other 21 appendices. MajorHeadId is an external ReferenceData id.
/// </summary>
public class PublicAccountReceiptPayment : AppendixEntityBase
{
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

    public void UpdateFrom(
        int? majorHeadId,
        decimal? actualReceipt,
        decimal? actualPayment,
        decimal? balanceAtEndReceipt,
        decimal? balanceAtEndPayment,
        decimal? beReceipt,
        decimal? bePayment,
        decimal? adjustmentReceipt,
        decimal? adjustmentPayment,
        decimal? reReceipt,
        decimal? rePayment,
        decimal? nbeReceipt,
        decimal? nbePayment,
        string? remarksReceipt,
        string? remarksPayment)
    {
        MajorHeadId = majorHeadId;
        ActualReceipt = actualReceipt;
        ActualPayment = actualPayment;
        BalanceAtEndReceipt = balanceAtEndReceipt;
        BalanceAtEndPayment = balanceAtEndPayment;
        BEReceipt = beReceipt;
        BEPayment = bePayment;
        AdjustmentReceipt = adjustmentReceipt;
        AdjustmentPayment = adjustmentPayment;
        REReceipt = reReceipt;
        REPayment = rePayment;
        NBEReceipt = nbeReceipt;
        NBEPayment = nbePayment;
        RemarksReceipt = remarksReceipt;
        RemarksPayment = remarksPayment;
    }
}
