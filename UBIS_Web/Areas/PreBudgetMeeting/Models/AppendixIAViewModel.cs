namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixIAViewModel : AppendixBaseViewModel
{
    public List<AppendixProjectedDemandDto> Records { get; set; } = new();
    public SaveAppendixProjectedDemandDto NewRecord { get; set; } = new();
}
