namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-E: Autonomous Bodies for which Corpus Fund has been created out of GiA support. Maps to dbo.AppendixCorpusFundAbGia (was legacy Temp_CorpusFundABGiA).</summary>
public class AppendixCorpusFundAbGia : AppendixEntityBase
{
    public int AutonomousBodyId { get; set; }
    public decimal? CorpusFundBalance1 { get; set; }
    public decimal? CorpusFundBalance2 { get; set; }
    public string? CorpusFundBankName { get; set; }
    public string? CorpusFundReason { get; set; }
    public decimal? GiaRE { get; set; }
    public decimal? GiaBE { get; set; }

    public void UpdateFrom(
        int autonomousBodyId,
        decimal? corpusFundBalance1,
        decimal? corpusFundBalance2,
        string? corpusFundBankName,
        string? corpusFundReason,
        decimal? giaRE,
        decimal? giaBE)
    {
        AutonomousBodyId = autonomousBodyId;
        CorpusFundBalance1 = corpusFundBalance1;
        CorpusFundBalance2 = corpusFundBalance2;
        CorpusFundBankName = corpusFundBankName;
        CorpusFundReason = corpusFundReason;
        GiaRE = giaRE;
        GiaBE = giaBE;
    }
}
