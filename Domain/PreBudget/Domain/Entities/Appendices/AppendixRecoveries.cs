namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VII-A: Statement of recoveries taken in reduction of expenditure under a Major Head. Maps to dbo.AppendixRecoveries (was legacy Temp_Recoveries). MajorHeadId is an external ReferenceData id.</summary>
public class AppendixRecoveries : AppendixEntityBase
{
    public string? SchemeName { get; set; }
    public int? MajorHeadId { get; set; }
    public bool IsCharged { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }

    public void UpdateFrom(
        string? schemeName,
        int? majorHeadId,
        bool isCharged,
        decimal? actuals,
        decimal? be,
        decimal? re,
        decimal? nbe)
    {
        SchemeName = schemeName;
        MajorHeadId = majorHeadId;
        IsCharged = isCharged;
        Actuals = actuals;
        BE = be;
        RE = re;
        NBE = nbe;
    }
}
