namespace UBIS.Services.ReferenceData.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_MajorHead table (SQL Queries\new-tables\ReferenceData_CreateTables.sql).
/// Trimmed from the legacy BIMSDemo.M_MajorHead (which also carries Debt-module-specific
/// MajorHeadGroup1-4Id/RE_Debt grouping columns not needed by any Pre-Budget appendix).
/// </summary>
public class MajorHead
{
    public int MajorHeadId { get; set; }

    public string MajorHeadCode { get; set; } = string.Empty;

    public string MajorHeadName { get; set; } = string.Empty;

    public string? HMajorHeadName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
