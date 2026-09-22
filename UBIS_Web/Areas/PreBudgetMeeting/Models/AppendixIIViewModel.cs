namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>Appendix II: Quarterly Expenditure Plan (QEP) — includes the FRS's Deviation Information fields, confirmed absent from both the old app's table and the designer prototype's form (see evaluation doc §11.3).</summary>
public class AppendixIIViewModel : AppendixBaseViewModel
{
    public List<AppendixQuarterlyExpenditurePlanDto> Records { get; set; } = new();

    public SaveAppendixQuarterlyExpenditurePlanDto NewRecord { get; set; } = new();
}
