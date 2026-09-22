namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>Appendix III-A: TSA Assignment and Expenditure — the screenshot-confirmed reference appendix (evaluation doc §5).</summary>
public class AppendixIIIAViewModel : AppendixBaseViewModel
{
    public List<AppendixTsaAssignmentDto> Records { get; set; } = new();

    // Single-row entry form (client review 2026-08-05) - was 2 blank rows per the original approved
    // design; the client asked for one row of input boxes, matching every other appendix's NewRecord
    // pattern (add one entity, Submit, it appears in the grid below).
    public SaveAppendixTsaAssignmentDto NewRecord { get; set; } = new();
}
