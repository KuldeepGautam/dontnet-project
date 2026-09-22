namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIIAViewModel : AppendixBaseViewModel
{
    public List<AppendixRecoveriesDto> Records { get; set; } = new();
    public SaveAppendixRecoveriesDto NewRecord { get; set; } = new();
    public List<AppendixVIIAMajorHeadOptionDto> MajorHeads { get; set; } = new();
}
