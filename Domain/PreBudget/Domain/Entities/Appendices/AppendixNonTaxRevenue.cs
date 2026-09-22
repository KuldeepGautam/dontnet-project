namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI: Non-Tax Revenue / Capital Receipts. Maps to dbo.Appendix_VI_NonTaxRevenue.</summary>
public class AppendixNonTaxRevenue : AppendixEntityBase
{
    /// <summary>FK -> dbo.M_Appendix_VI_ReceiptType (client re-design 2026-09-10). <see cref="ReceiptType"/>
    /// carries the resolved name for grid/export/search.</summary>
    public int? ReceiptTypeId { get; set; }
    public string? ReceiptType { get; set; }
    public string? PsuReceiptName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    /// <summary>"Proposed B.E. (next FY)" - column was dbo...ProposedRE, renamed to ProposedBE in the
    /// 2026-09-10 re-design (Appendix6.html replaces "Proposed R.E." with "Proposed B.E.").</summary>
    public decimal? ProposedBE { get; set; }
    public decimal? ProposedCollectionQ3 { get; set; }
    public decimal? ProposedCollectionQ4 { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        int? receiptTypeId,
        string? receiptType,
        string? psuReceiptName,
        decimal? actuals,
        decimal? be,
        decimal? actualsUptoSept,
        decimal? proposedBE,
        decimal? proposedCollectionQ3,
        decimal? proposedCollectionQ4,
        string? remarks)
    {
        ReceiptTypeId = receiptTypeId;
        ReceiptType = receiptType;
        PsuReceiptName = psuReceiptName;
        Actuals = actuals;
        BE = be;
        ActualsUptoSept = actualsUptoSept;
        ProposedBE = proposedBE;
        ProposedCollectionQ3 = proposedCollectionQ3;
        ProposedCollectionQ4 = proposedCollectionQ4;
        Remarks = remarks;
    }
}
