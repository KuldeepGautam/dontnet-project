namespace UBIS.Services.ReferenceData.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_ObjectHead table. Gets a proper identity PK, unlike the legacy
/// BIMSDemo.ObjectHead (which has no surrogate key, only composite Category/SubCategory/
/// ObjectHead codes).
/// </summary>
public class ObjectHead
{
    public int ObjectHeadId { get; set; }

    public string ObjectHeadCode { get; set; } = string.Empty;

    public string ObjectHeadName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
