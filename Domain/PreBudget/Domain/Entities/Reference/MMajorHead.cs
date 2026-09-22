namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>Read-only reference data mapped onto the shared dbo.M_MajorHead table - see MCategory's doc comment. Populated 2026-08-05 from legacy BIMSDemo.dbo.M_MajorHead (was previously an empty table).</summary>
public class MMajorHead
{
    public int MajorHeadId { get; set; }
    public string MajorHeadCode { get; set; } = string.Empty;
    public string? MajorHeadName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
