namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to the new dbo.PreBudgetRemark table (FR-003) — replaces legacy Temp_Remarks. Per the FRS
/// §3.1.2 Access Control Matrix, only Budget Division and ABO/DS/Director users may create or view
/// these — Ministry/Department users are blocked entirely. That role gate is enforced in
/// PreBudget.Application (RemarkService), not here or at the DB level.
/// </summary>
public class Remark
{
    public int Id { get; set; }

    public int DemandId { get; set; }

    public int AppendixId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public string RemarkText { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    /// <summary>Snapshot of the creating user's role name at the time of writing — the FRS requires
    /// remarks to record "the name/role of the user recording it", which must survive later role
    /// changes.</summary>
    public string? CreatedByRoleSnapshot { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
}
