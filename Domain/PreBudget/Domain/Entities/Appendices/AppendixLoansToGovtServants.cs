namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix X: Loan to Government Servants, etc. (Major Head 7610). Maps to dbo.AppendixLoansToGovtServants (was legacy Temp_Loans_GovtServants).</summary>
public class AppendixLoansToGovtServants : AppendixEntityBase
{
    public string? SubHeadName { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY3 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }

    public void UpdateFrom(
        string? subHeadName,
        decimal? actualsY1,
        decimal? actualsY2,
        decimal? actualsY3,
        decimal? actualsUptoSept,
        decimal? be,
        decimal? re,
        decimal? nbe)
    {
        SubHeadName = subHeadName;
        ActualsY1 = actualsY1;
        ActualsY2 = actualsY2;
        ActualsY3 = actualsY3;
        ActualsUptoSept = actualsUptoSept;
        BE = be;
        RE = re;
        NBE = nbe;
    }
}
