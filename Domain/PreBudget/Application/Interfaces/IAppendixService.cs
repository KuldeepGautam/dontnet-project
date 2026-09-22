namespace UBIS.Services.PreBudget.Application.Interfaces;

using UBIS.Services.PreBudget.Application.DTOs;

/// <summary>
/// Powers the Select-Demand-and-Appendix screen (replaces the old sp_Select_Demands_Template) and
/// the FRS Dashboard's "Status" widget. Permission filtering (which Demands a user may see) is the
/// caller's (UBIS_Web's) responsibility via M_MapUserRole — this service is handed a DemandId
/// it should already trust.
/// </summary>
public interface IAppendixService
{
    Task<IReadOnlyList<AppendixDto>> GetAppendicesAsync(string financialYear, CancellationToken ct);

    Task<IReadOnlyList<AppendixWithStatusDto>> GetAppendicesWithStatusAsync(int demandId, string financialYear, CancellationToken ct);

    Task SetNilSubmissionAsync(SetNilSubmissionDto request, int userId, CancellationToken ct);

    Task FreezeSubmissionAsync(int demandId, int appendixId, string financialYear, int userId, CancellationToken ct);

    /// <summary>Bulk-assigns TargetDate to (DemandId x AppendixId) pairs - the "Add Allocation" screen's submit action. bearerToken is forwarded to AIM when resolving notification recipients.</summary>
    Task<AllocationResultDto> AllocateAsync(AllocateAppendixDto request, int userId, string bearerToken, CancellationToken ct);

    Task<IReadOnlyList<AllocationListItemDto>> GetAllocationsAsync(int? appendixId, string financialYear, CancellationToken ct);

    /// <summary>Row-level edit for one Demand's allocation - see UpdateAllocationDto's doc comment.</summary>
    Task UpdateAllocationAsync(UpdateAllocationDto request, int userId, CancellationToken ct);

    /// <summary>
    /// Client report 2026-09-10 ("Demand dropdown showing 3 entries per demand"): filters a
    /// caller-supplied candidate DemandId list (already permission-checked against M_MapUserRole by
    /// AIM/UBIS_Web) down to only Demands that actually have at least one dbo.M_DemandAppendixAllocation
    /// row for this FinancialYear - i.e. a Demand this appendix-allocation workflow has actually been
    /// set up for. DemandAllocation is one row per (Demand, Appendix, FinancialYear) (see that
    /// entity's own doc comment) - a naive INNER JOIN from Demand to it would return one row PER
    /// allocated Appendix (exactly the reported "3 entries per demand" symptom for a Demand with 3
    /// allocated appendixes). Returns DISTINCT DemandIds instead, so each qualifying Demand appears
    /// exactly once regardless of how many Appendixes it has allocated.
    /// </summary>
    Task<IReadOnlyList<int>> GetAllocatedDemandIdsAsync(IReadOnlyList<int> demandIds, string financialYear, CancellationToken ct);
}
