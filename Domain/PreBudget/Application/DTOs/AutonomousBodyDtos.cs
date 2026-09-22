namespace UBIS.Services.PreBudget.Application.DTOs;

public class AutonomousBodyDto
{
    public int AutonomousBodyId { get; set; }
    public int DemandId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
}

/// <summary>FR-004: Administrator-only direct add (client design, 2026-08-10, matches the legacy "Add Autonomous/Grantee Name" form) - creates the M_AutonomousBody row immediately, no request/approval step, since the Administrator is the same person who'd otherwise approve it.</summary>
public class CreateAutonomousBodyDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
}

/// <summary>FR-004: Ministry user requests a new Autonomous Body when theirs isn't listed.</summary>
public class CreateAutonomousBodyRequestDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RequestedName { get; set; } = string.Empty;
}

public class AutonomousBodyRequestDto
{
    public int RequestId { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RequestedName { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
}

/// <summary>FR-004: Administrator approves/rejects a pending request. Approving creates the real M_AutonomousBody row.</summary>
public class ReviewAutonomousBodyRequestDto
{
    public bool Approve { get; set; }
    public string? ReviewRemarks { get; set; }
    public string? Code { get; set; }
}
