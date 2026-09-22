namespace UBIS.Services.PreBudget.Application.Interfaces;

using UBIS.Services.PreBudget.Application.DTOs;

/// <summary>
/// FR-003: RE Data Remarks. Per the FRS §3.1.2 Access Control Matrix, only Budget Division and
/// ABO/DS/Director users may view or create these — Ministry/Department users must be blocked
/// entirely, a real behavior change vs the old app's sp_Insert_TemplateRemarks (which had no role
/// check at all). callerRoleName is passed in from the controller's JWT claims; the allow-list is
/// configurable via RemarkRoleOptions since the exact M_Role seed values for these three FRS roles
/// weren't confirmed this pass.
/// </summary>
public interface IRemarkService
{
    Task<IReadOnlyList<RemarkDto>> GetRemarksAsync(int demandId, int appendixId, string financialYear, string callerRoleName, CancellationToken ct);

    Task<RemarkDto> CreateRemarkAsync(CreateRemarkDto request, int userId, string callerRoleName, CancellationToken ct);
}

/// <summary>Thrown when a Ministry/Department user (or any role not in RemarkRoleOptions.AllowedRoleNames) attempts to view/create a remark.</summary>
public class RemarkAccessDeniedException : Exception
{
    public RemarkAccessDeniedException() : base("Only Budget Division and ABO/DS/Director users may view or add RE Data Remarks.")
    {
    }
}
