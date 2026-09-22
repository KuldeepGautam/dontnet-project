namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VII-B: Commercial receipts of departmentally-run commercial undertakings. Maps to dbo.AppendixCommercialUndertakingReceipts (was legacy Temp_VIIB_CommUnder). SchemeId/MajorHeadId are external ReferenceData ids.</summary>
public class AppendixCommercialUndertakingReceipts : AppendixEntityBase
{
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

    public void UpdateFrom(
        int? schemeId,
        string? transactionType,
        int? majorHeadId,
        decimal? actualsY2,
        decimal? actualsY1,
        decimal? actualsUptoSept,
        decimal? actualsUptoSeptPrevYear,
        decimal? be,
        decimal? re,
        decimal? increasedBE,
        decimal? nbe)
    {
        SchemeId = schemeId;
        TransactionType = transactionType;
        MajorHeadId = majorHeadId;
        ActualsY2 = actualsY2;
        ActualsY1 = actualsY1;
        ActualsUptoSept = actualsUptoSept;
        ActualsUptoSeptPrevYear = actualsUptoSeptPrevYear;
        BE = be;
        RE = re;
        IncreasedBE = increasedBE;
        NBE = nbe;
    }
}
