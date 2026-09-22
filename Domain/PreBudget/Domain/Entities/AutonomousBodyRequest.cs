namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_AutonomousBodyRequest table (FR-004) — the "Ministry user requests a new
/// Autonomous Body, subject to Administrator approval" workflow. Genuinely new, no equivalent in
/// the old app.
/// </summary>
public class AutonomousBodyRequest
{
    public int RequestId { get; set; }

    public int DemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public string RequestedName { get; set; } = string.Empty;

    public int RequestedByUserId { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public string Status { get; set; } = AutonomousBodyRequestStatus.Pending;

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewRemarks { get; set; }

    public int? ApprovedAutonomousBodyId { get; set; }
}

public static class AutonomousBodyRequestStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
