namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixIVViewModel : AppendixBaseViewModel
{
    public List<AppendixEstimatesOfSchemesDto> Records { get; set; } = new();
    public SaveAppendixEstimatesOfSchemesDto NewRecord { get; set; } = new();
    public List<AppendixIIISchemeOptionDto> Schemes { get; set; } = new();
}
