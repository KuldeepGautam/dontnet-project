namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Maps the newly-created dbo.M_SubCategory (Others/publish-staging/sbe-workstream-1-schema.sql).
/// Not year-versioned — confirmed via direct BIMSDemo verification (2026-08-25) that the legacy
/// table has no FinancialYear column, unlike M_Category. Foundation-stage: plain data holder;
/// FR021 Masters-Add-Sub-Category write behavior is added in Stage 2.
/// </summary>
public class SbeSubCategory
{
    public int SubCategoryId { get; set; }
    public int? CategoryId { get; set; }
    public string? SerialNo { get; set; }
    public string? SubCategoryName { get; set; }
    public string? HSubCategoryName { get; set; }
    public string? Active { get; set; }
    public string? Remarks { get; set; }
    public DateTime? EntryDate { get; set; }
    public int? PrevSubCategoryId { get; set; }
    public string? Ip { get; set; }
    public int? UserId { get; set; }
}
