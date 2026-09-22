namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixCorpusFundAbGiaDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public decimal? CorpusFundBalance1 { get; set; }
    public decimal? CorpusFundBalance2 { get; set; }
    public string? CorpusFundBankName { get; set; }
    public string? CorpusFundReason { get; set; }
    public decimal? GiaRE { get; set; }
    public decimal? GiaBE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixCorpusFundAbGiaDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public decimal? CorpusFundBalance1 { get; set; }
    public decimal? CorpusFundBalance2 { get; set; }
    public string? CorpusFundBankName { get; set; }
    public string? CorpusFundReason { get; set; }
    public decimal? GiaRE { get; set; }
    public decimal? GiaBE { get; set; }
}
