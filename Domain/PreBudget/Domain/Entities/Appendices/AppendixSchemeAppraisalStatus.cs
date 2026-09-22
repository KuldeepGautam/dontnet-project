namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix III-B: Status of Scheme Appraisal/Approval during the XVI Finance Commission
/// Cycle. Maps to dbo.Appendix_IIIB_SchemeAppraisalStatus. New appendix, added 2026-09-09 per
/// Appendix_Papes_I_to IIIB/Appendix-III-B.html.
///
/// Category and Scheme changed from free text to real cascading dropdowns 2026-09-10 (client
/// instruction): Category from dbo.M_Category (current FY, SerialNo II/IV), Scheme cascading from
/// the chosen Category (M_Scheme by Demand + CategoryId, same as Appendix III). <see cref="CategoryId"/>
/// / <see cref="SchemeId"/> are what the record is now keyed on for uniqueness and Edit-repopulate;
/// <see cref="CategoryType"/> / <see cref="SchemeName"/> are kept as the resolved *display names*
/// (grid/export/search) written from the chosen ids at save time.</summary>
public class AppendixSchemeAppraisalStatus : AppendixEntityBase
{
    public int? CategoryId { get; set; }
    public int? SchemeId { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string StatusOfFreshAppraisalApproval { get; set; } = string.Empty;
    public DateOnly? SchemeApprovalValidUpto { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        int? categoryId,
        int? schemeId,
        string categoryType,
        string schemeName,
        string statusOfFreshAppraisalApproval,
        DateOnly? schemeApprovalValidUpto,
        string? remarks)
    {
        CategoryId = categoryId;
        SchemeId = schemeId;
        CategoryType = categoryType;
        SchemeName = schemeName;
        StatusOfFreshAppraisalApproval = statusOfFreshAppraisalApproval;
        SchemeApprovalValidUpto = schemeApprovalValidUpto;
        Remarks = remarks;
    }
}
