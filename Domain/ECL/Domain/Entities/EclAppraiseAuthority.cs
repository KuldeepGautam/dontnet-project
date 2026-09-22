namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Appraisal Authority master. Maps dbo.M_ECLAppraiseAuthority (renamed from dbo.M_EclApprised,
/// client request 2026-08-25). Seeded with the same 5 initial values as EclApprovalAuthority
/// (Financial Advisor, SFC, DIB, PIB, EFC) but is now an independent list that can diverge going
/// forward — see EclApprovalAuthority's doc comment for the full "why two tables" context and the
/// "Others" promotion behavior (EclOutlayRepository.EnsureAppraiseAuthorityAsync).
/// </summary>
public class EclAppraiseAuthority
{
    public int AppraiseId { get; set; }

    public string AppraiseName { get; set; } = string.Empty;

    public bool IsDropdownVisible { get; set; } = true;

    public int? DisplaySequenceNo { get; set; }

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }

    public bool IsDeleted { get; set; }
}
