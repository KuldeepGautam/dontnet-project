namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>FR010's Add PSE Category master. Maps the newly-created dbo.PSECategory.</summary>
public class PseCategory
{
    public int PseCategoryId { get; set; }
    public string? FinancialYear { get; set; }
    public int? DemandId { get; set; }
    public int? DemandNo { get; set; }
    public int? PseCategoryCode { get; set; }
    public string? PseCategoryName { get; set; }
    public string? HPseCategoryName { get; set; }
    public string? Active { get; set; }
    public DateTime? EntryDate { get; set; }
    public int? PrevPseCategoryId { get; set; }
    public string? Ip { get; set; }
    public int? UserId { get; set; }
}
