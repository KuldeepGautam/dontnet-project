namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only reference entity mapping dbo.M_Category (FY-scoped). Only rows with SerialNo IN ('II','IV') apply to this module — "Categories 2 and 4".</summary>
public class EclCategory
{
    public int CategoryId { get; set; }

    public string? FinancialYear { get; set; }

    public string? SerialNo { get; set; }

    public string? CategoryName { get; set; }

    public string? HCategoryName { get; set; }

    public bool IsActive { get; set; }

    public int? PrevCategoryId { get; set; }

    public bool IsDeleted { get; set; }
}
