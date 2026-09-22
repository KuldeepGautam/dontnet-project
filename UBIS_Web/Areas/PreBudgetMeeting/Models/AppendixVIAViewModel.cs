namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIAViewModel : AppendixBaseViewModel
{
    public List<AppendixUserChargesDto> Records { get; set; } = new();
    public SaveAppendixUserChargesDto NewRecord { get; set; } = new();
}
