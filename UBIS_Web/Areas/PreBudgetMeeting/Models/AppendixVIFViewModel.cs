namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIFViewModel : AppendixBaseViewModel
{
    public List<AppendixMinorHeadUserChargesDto> Records { get; set; } = new();
    public SaveAppendixMinorHeadUserChargesDto NewRecord { get; set; } = new();
    /// <summary>Resolved name of NewRecord.MinorHeadCode, for the Edit path's pre-filled label (the
    /// Add path resolves this live via AJAX as the user types/picks - see appendix-vif-drawer.js).</summary>
    public string? NewRecordMinorHeadName { get; set; }
}
