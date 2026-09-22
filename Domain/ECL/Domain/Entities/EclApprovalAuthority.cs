namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Approval Authority master. Maps dbo.M_ECLApprovalAuthority (renamed from dbo.M_EclAuthority,
/// client request 2026-08-25 — previously this single table backed BOTH the "Approval Authority"
/// and "Appraisal Authority" dropdowns on Add Scheme Outlay, which was wrong; they're now two
/// independent master tables — see EclAppraiseAuthority for the other one). "Others" is a UI-only
/// sentinel on the dropdown itself; when a user types a genuinely new value there, it's promoted
/// into this table with IsDropdownVisible = false (see EclOutlayRepository.EnsureApprovalAuthorityAsync)
/// so it's saved as a real row, not just free text, but doesn't clutter the dropdown for other users.
/// </summary>
public class EclApprovalAuthority
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
