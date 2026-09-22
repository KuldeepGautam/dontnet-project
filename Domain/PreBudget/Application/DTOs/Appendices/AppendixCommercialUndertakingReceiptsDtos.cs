namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixCommercialUndertakingReceiptsDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public string? TransactionType { get; set; }
    public int? MajorHeadId { get; set; }
    public string? MajorHeadCode { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? IncreasedBE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

// NOT [NonNegativeAmounts] (2026-08-07): unlike every other appendix's amount fields, VII-B's
// "Revenue Receipts" transaction-type rows are legitimately negative in real data (confirmed
// against a live screenshot of this exact page - e.g. BE/Actuals/RE/NBE all negative for a
// Revenue Receipts row) - the blanket non-negative rule would incorrectly reject real saves here.
public class SaveAppendixCommercialUndertakingReceiptsDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? TransactionType { get; set; }
    public int? MajorHeadId { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? IncreasedBE { get; set; }
    public decimal? NBE { get; set; }
}
