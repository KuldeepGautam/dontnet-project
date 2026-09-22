namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIEViewModel : AppendixBaseViewModel
{
    public List<AppendixCorpusFundAbGiaDto> Records { get; set; } = new();
    public SaveAppendixCorpusFundAbGiaDto NewRecord { get; set; } = new();
    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();
}
