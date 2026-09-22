namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-G: User Charges of Autonomous Bodies / various organizations - keyed by
/// Autonomous Body (one row per Demand+FinancialYear+AutonomousBody, same uniqueness convention as
/// VI-C/VI-E). New appendix, added 2026-09-09 per UBIS-Pre-budget-html/Appendix6g.html.</summary>
public class AppendixUserChargesAutonomousBody : AppendixEntityBase
{
    public int AutonomousBodyId { get; set; }
    public string? BriefOnRevenueSources { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? ReceiptsCollected { get; set; }
    public decimal? TotalRevenueExpenditure { get; set; }
    public decimal? TotalCapitalExpenditure { get; set; }

    public void UpdateFrom(
        int autonomousBodyId,
        string? briefOnRevenueSources,
        string? presentStatus,
        decimal? receiptsCollected,
        decimal? totalRevenueExpenditure,
        decimal? totalCapitalExpenditure)
    {
        AutonomousBodyId = autonomousBodyId;
        BriefOnRevenueSources = briefOnRevenueSources;
        PresentStatus = presentStatus;
        ReceiptsCollected = receiptsCollected;
        TotalRevenueExpenditure = totalRevenueExpenditure;
        TotalCapitalExpenditure = totalCapitalExpenditure;
    }
}
