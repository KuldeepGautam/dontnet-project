namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

// Appendix V-B: Details of Establishment Expenditure - Object Head wise. Split 2026-09-16 out of
// the former shared AppendixVGroupViewModel - see AppendixVAViewModel's header comment.
public class AppendixVBViewModel : AppendixBaseViewModel
{
    public List<AppendixEstablishmentByObjectHeadDto> VBRecords { get; set; } = new();
    public SaveAppendixEstablishmentByObjectHeadDto VBNewRecord { get; set; } = new();
    public List<AppendixVBObjectHeadOptionDto> ObjectHeads { get; set; } = new();

    public bool IsVBFrozen { get; set; }
    public bool IsVBLocked { get; set; }
}
