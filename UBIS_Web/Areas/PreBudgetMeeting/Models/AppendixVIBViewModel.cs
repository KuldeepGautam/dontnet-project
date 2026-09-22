namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIBViewModel : AppendixBaseViewModel
{
    public List<AppendixPendingLiabilitiesDto> Records { get; set; } = new();
    public SaveAppendixPendingLiabilitiesDto NewRecord { get; set; } = new();

    // Category isn't part of the saved record (the legacy sp_Temp_PendingLiabilities never
    // persisted one either) - it exists purely to drive the Category -> Scheme -> SubScheme
    // cascade the approved design calls for (added 2026-07-30), so it lives on the view model
    // directly rather than on NewRecord/SaveAppendixPendingLiabilitiesDto.
    public int? CategoryId { get; set; }
    public List<AppendixVIBCategoryOptionDto> Categories { get; set; } = new();
    public List<AppendixIIISchemeOptionDto> Schemes { get; set; } = new();
}
