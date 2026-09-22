namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIGViewModel : AppendixBaseViewModel
{
    public List<AppendixUserChargesAutonomousBodyDto> Records { get; set; } = new();
    public SaveAppendixUserChargesAutonomousBodyDto NewRecord { get; set; } = new();
    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();
}
