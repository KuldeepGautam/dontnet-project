namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Maps to dbo.M_EclAuthority — approval/appraisal authority master, seeded with 8 rows. "Others" is a UI-only sentinel, never persisted here.</summary>
public class EclAuthority
{
    public int AuthorityId { get; set; }

    public string AuthorityName { get; set; } = string.Empty;

    public bool IsDropdownVisible { get; set; } = true;

    public int? DisplaySequenceNo { get; set; }

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }

    public bool IsDeleted { get; set; }
}
