namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVICViewModel : AppendixBaseViewModel
{
    public List<AppendixCorpusFundDto> Records { get; set; } = new();
    public SaveAppendixCorpusFundDto NewRecord { get; set; } = new();
    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();
}
