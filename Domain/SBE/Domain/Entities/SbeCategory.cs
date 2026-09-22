namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>Maps dbo.M_Category (FY-scoped, shared with ECL/PreBudget). Foundation-stage: plain data holder for reads/joins only — FR020 Masters-Add-Category write behavior is added in Stage 2.</summary>
public class SbeCategory
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
