namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>Read-only reference data mapped onto the shared dbo.M_ObjectHead table - see MCategory's doc comment.</summary>
public class MObjectHead
{
    public int ObjectHeadId { get; set; }
    public string? ObjectHeadCode { get; set; }
    public string? ObjectHeadName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
