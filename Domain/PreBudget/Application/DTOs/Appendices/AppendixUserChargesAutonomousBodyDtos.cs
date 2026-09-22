namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

public class AppendixUserChargesAutonomousBodyDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public string? BriefOnRevenueSources { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? ReceiptsCollected { get; set; }
    public decimal? TotalRevenueExpenditure { get; set; }
    public decimal? TotalCapitalExpenditure { get; set; }
    public bool IsFrozen { get; set; }
}

// Client requirement 2026-09-09: "All fields are required" - every field on this appendix's form
// carries [Required] so a blank/missing value is rejected server-side too (not just via the
// client's own asp-for required attribute), same "never enforce only in the UI" rule as every
// other appendix's validation.
[NonNegativeAmounts]
public class SaveAppendixUserChargesAutonomousBodyDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    [Required]
    public int AutonomousBodyId { get; set; }
    [Required]
    [StringLength(500)]
    public string? BriefOnRevenueSources { get; set; }
    [Required]
    [StringLength(500)]
    public string? PresentStatus { get; set; }
    [Required]
    public decimal? ReceiptsCollected { get; set; }
    [Required]
    public decimal? TotalRevenueExpenditure { get; set; }
    [Required]
    public decimal? TotalCapitalExpenditure { get; set; }
}
