namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

public class AppendixInternalResourcesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public decimal? AsOnMarch31 { get; set; }
    public decimal? AsOnJune30 { get; set; }
    public decimal? ExpectedNextMarch31 { get; set; }
    public decimal? ExpectedNextFY { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixInternalResourcesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? AutonomousBodyId { get; set; }

    // Client requirement 2026-08-27: "all numeric fields are required" - was already HTML
    // `required` on the entry form, but that's not authoritative on its own (this codebase's
    // "server-side enforcement, never UI-hiding alone" rule); [Required] makes it so here too.
    [Required(ErrorMessage = "Enter value for As on 31st March.")]
    public decimal? AsOnMarch31 { get; set; }

    [Required(ErrorMessage = "Enter value for As on 30th September.")]
    public decimal? AsOnJune30 { get; set; }

    [Required(ErrorMessage = "Enter value for Expected upto 31st March.")]
    public decimal? ExpectedNextMarch31 { get; set; }

    [Required(ErrorMessage = "Enter value for Expected (current FY).")]
    public decimal? ExpectedNextFY { get; set; }

    public string? Remarks { get; set; }
}
