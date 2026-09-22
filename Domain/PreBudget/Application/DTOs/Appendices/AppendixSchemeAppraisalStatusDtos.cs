namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

public class AppendixSchemeAppraisalStatusDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? SchemeId { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string StatusOfFreshAppraisalApproval { get; set; } = string.Empty;
    public DateOnly? SchemeApprovalValidUpto { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixSchemeAppraisalStatusDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    // Category and Scheme are now real cascading dropdowns (client instruction 2026-09-10) - the
    // client sends the chosen ids; the controller resolves CategoryType/SchemeName display strings
    // from them, so those are no longer [Required] on the wire.
    [Required]
    public int? CategoryId { get; set; }
    [Required]
    public int? SchemeId { get; set; }

    [StringLength(250)]
    public string? CategoryType { get; set; }
    [StringLength(500)]
    public string? SchemeName { get; set; }

    [Required]
    [StringLength(500)]
    public string StatusOfFreshAppraisalApproval { get; set; } = string.Empty;
    public DateOnly? SchemeApprovalValidUpto { get; set; }
    [StringLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>Category dropdown option for Appendix III-B - dbo.M_Category rows for the current FY
/// with SerialNo II or IV (client reference query 2026-09-10).</summary>
public class AppendixIIIBCategoryOptionDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
