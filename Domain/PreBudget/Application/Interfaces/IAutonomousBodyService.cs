namespace UBIS.Services.PreBudget.Application.Interfaces;

using UBIS.Services.PreBudget.Application.DTOs;

/// <summary>FR-004: Autonomous Master for Appendix VI-C/VI-D/VI-E, plus the request/approval workflow.</summary>
public interface IAutonomousBodyService
{
    Task<IReadOnlyList<AutonomousBodyDto>> GetAutonomousBodiesAsync(int demandId, string financialYear, CancellationToken ct);

    /// <summary>
    /// Appendix V-A's dropdown only (client review 2026-08-06): "select distinct Name from
    /// M_AutonomousBody where DemandId=@DemandId order by Name" - scoped to Demand only, no
    /// FinancialYear/IsActive filter, deduplicated by Name. Deliberately separate from
    /// GetAutonomousBodiesAsync above, which VI-C/VI-E still use unchanged.
    /// </summary>
    Task<IReadOnlyList<AutonomousBodyDto>> GetAutonomousBodiesForAppendixVAAsync(int demandId, string financialYear, CancellationToken ct);

    /// <summary>Administrator-only — see AutonomousBodyAdminRoleOptions. Direct add, no request/approval step.</summary>
    Task<AutonomousBodyDto> CreateAutonomousBodyAsync(CreateAutonomousBodyDto request, int userId, string callerRoleName, CancellationToken ct);

    Task<AutonomousBodyRequestDto> RequestNewAutonomousBodyAsync(CreateAutonomousBodyRequestDto request, int userId, CancellationToken ct);

    /// <summary>Administrator-only — see AutonomousBodyAdminRoleOptions.</summary>
    Task<IReadOnlyList<AutonomousBodyRequestDto>> GetPendingRequestsAsync(string callerRoleName, CancellationToken ct);

    /// <summary>Administrator-only. Approving creates the real M_AutonomousBody row and links it back to the request.</summary>
    Task<AutonomousBodyRequestDto> ReviewRequestAsync(int requestId, ReviewAutonomousBodyRequestDto review, int reviewerUserId, string callerRoleName, CancellationToken ct);
}

/// <summary>Thrown when a non-Administrator role attempts to view pending requests or review one.</summary>
public class AutonomousBodyAccessDeniedException : Exception
{
    public AutonomousBodyAccessDeniedException() : base("Only Administrators may view or review Autonomous Body requests.")
    {
    }
}
