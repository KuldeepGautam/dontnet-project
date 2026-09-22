namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>FR-004: Autonomous Master for Appendix VI-C/VI-D/VI-E. Server-side role gate on the review actions is authoritative (AutonomousBodiesController); CanReview is a UI convenience only.</summary>
public class AutonomousMasterViewModel
{
    public List<DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();

    public List<AutonomousBodyRequestDto> PendingRequests { get; set; } = new();

    public bool CanReview { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
