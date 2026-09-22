namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixIVBViewModel : AppendixBaseViewModel
{
    public List<AppendixTaspExpenditureDto> Records { get; set; } = new();
    public SaveAppendixTaspExpenditureDto NewRecord { get; set; } = new();
    public List<AppendixIIISchemeOptionDto> Schemes { get; set; } = new();
}
