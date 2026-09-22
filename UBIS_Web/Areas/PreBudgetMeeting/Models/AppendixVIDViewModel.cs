namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIDViewModel : AppendixBaseViewModel
{
    public List<AppendixInternalResourcesDto> Records { get; set; } = new();
    public SaveAppendixInternalResourcesDto NewRecord { get; set; } = new();
    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();
}
