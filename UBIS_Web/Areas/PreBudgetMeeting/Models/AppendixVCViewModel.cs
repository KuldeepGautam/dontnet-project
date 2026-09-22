namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

// Appendix V-C: Details of Establishment Expenditure - Other than AB. Split 2026-09-16 out of the
// former shared AppendixVGroupViewModel - see AppendixVAViewModel's header comment.
public class AppendixVCViewModel : AppendixBaseViewModel
{
    public List<AppendixEstablishmentOtherThanABDto> VCRecords { get; set; } = new();
    public SaveAppendixEstablishmentOtherThanABDto VCNewRecord { get; set; } = new();

    public bool IsVCFrozen { get; set; }
    public bool IsVCLocked { get; set; }
}
