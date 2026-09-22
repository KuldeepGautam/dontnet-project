namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix V-B: Establishment Expenditure - Object Head wise. Maps to dbo.AppendixEstablishmentByObjectHead (was legacy Temp_EstExp_ObjHeadwise). ObjectHeadId is an external ReferenceData id.</summary>
public class AppendixEstablishmentByObjectHead : AppendixEntityBase
{
    public int? ObjectHeadId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        int? objectHeadId,
        decimal? actuals,
        decimal? actualsUptoSeptPrevYear,
        decimal? be,
        decimal? actualsUptoSept,
        decimal? proposedRE,
        decimal? proposedNBE,
        string? remarks)
    {
        ObjectHeadId = objectHeadId;
        Actuals = actuals;
        ActualsUptoSeptPrevYear = actualsUptoSeptPrevYear;
        BE = be;
        ActualsUptoSept = actualsUptoSept;
        ProposedRE = proposedRE;
        ProposedNBE = proposedNBE;
        Remarks = remarks;
    }
}
