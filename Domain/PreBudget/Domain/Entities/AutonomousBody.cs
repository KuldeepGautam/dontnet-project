namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_AutonomousBody table (FR-004) — replaces legacy BIMSDemo.M_Autonomous.
/// Year-scoped and DemandId-scoped (decision 2). Required by Appendix VI-C/VI-D/VI-E's "Select
/// Autonomous" dropdowns.
/// </summary>
public class AutonomousBody
{
    public int AutonomousBodyId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public int DemandId { get; set; }

    /// <summary>Stable business demand identifier (unlike DemandId, doesn't change per financial year) - client review 2026-08-06, used to filter this table instead of DemandId since AIM's role-assignment DemandId claims can go stale across FY rollovers.</summary>
    public int? DemandNo { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? HName { get; set; }

    public int? PrevAutonomousBodyId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
