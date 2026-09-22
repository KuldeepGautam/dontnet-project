namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the shared dbo.M_Department table (DBA-owned, not part of
/// PreBudget's own bounded context - migrated 2026-08-03 from the legacy BIMSDemo database, same
/// pattern as MCategory/MScheme/MSubScheme, 2026-07-29). No FK to a Ministry table exists in
/// UBIS-Dev yet (there is no M_Ministry table here), so MinistryId stays a plain int column,
/// validated at the application layer only, same precedent as MScheme.DemandId having no FK to
/// M_Demand.
/// </summary>
public class MDepartment
{
    public int DepartmentId { get; set; }
    public int MinistryId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? HDepartmentName { get; set; }
    public string Active { get; set; } = "Y";
    public string? Remarks { get; set; }
    public DateTime? EntryDate { get; set; }

    /// <summary>Prior-year lineage, same pattern as MDemand.PrevDemandId / MCategory.PrevCategoryId.</summary>
    public int? PrevDepartmentId { get; set; }
}
