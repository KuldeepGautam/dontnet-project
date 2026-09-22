namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixNonTaxRevenueDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ReceiptTypeId { get; set; }
    public string? ReceiptType { get; set; }
    public string? PsuReceiptName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedBE { get; set; }
    public decimal? ProposedCollectionQ3 { get; set; }
    public decimal? ProposedCollectionQ4 { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixNonTaxRevenueDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ReceiptTypeId { get; set; }
    public string? ReceiptType { get; set; }
    public string? PsuReceiptName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedBE { get; set; }
    public decimal? ProposedCollectionQ3 { get; set; }
    public decimal? ProposedCollectionQ4 { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Option row for Appendix VI's "Receipt Type" dropdown - dbo.M_Appendix_VI_ReceiptType.</summary>
public class AppendixVIReceiptTypeOptionDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
