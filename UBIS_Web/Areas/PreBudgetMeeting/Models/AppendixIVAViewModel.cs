namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixIVAViewModel : AppendixBaseViewModel
{
    public List<AppendixScspExpenditureDto> Records { get; set; } = new();
    public SaveAppendixScspExpenditureDto NewRecord { get; set; } = new();
    public List<AppendixIIISchemeOptionDto> Schemes { get; set; } = new();
}
