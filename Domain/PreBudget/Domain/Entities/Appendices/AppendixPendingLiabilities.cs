namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-B: Pending Liabilities of Ministries. Maps to dbo.AppendixPendingLiabilities (was legacy Temp_PendingLiabilities).</summary>
public class AppendixPendingLiabilities : AppendixEntityBase
{
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? PendingLiabilityAsOnMarch31 { get; set; }
    public decimal? BE { get; set; }
    public decimal? EstimatedExpenditure { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        int? schemeId,
        int? subSchemeId,
        decimal? pendingLiabilityAsOnMarch31,
        decimal? be,
        decimal? estimatedExpenditure,
        string? remarks)
    {
        SchemeId = schemeId;
        SubSchemeId = subSchemeId;
        PendingLiabilityAsOnMarch31 = pendingLiabilityAsOnMarch31;
        BE = be;
        EstimatedExpenditure = estimatedExpenditure;
        Remarks = remarks;
    }
}
