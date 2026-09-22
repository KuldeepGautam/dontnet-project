namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

// Appendix V-A: Grant in Aid to Autonomous and other Bodies. Split 2026-09-16 out of the former
// shared AppendixVGroupViewModel (one tabbed page for V-A/V-B/V-C) into its own independent view
// model, matching every other standalone appendix - V-A/V-B/V-C were already split into 3
// standalone pages/controller files on 2026-09-14; this was the last shared piece left.
public class AppendixVAViewModel : AppendixBaseViewModel
{
    public List<AppendixGrantInAidDto> VARecords { get; set; } = new();
    public SaveAppendixGrantInAidDto VANewRecord { get; set; } = new();
    public List<AutonomousBodyDto> AutonomousBodies { get; set; } = new();

    public bool IsVAFrozen { get; set; }
    public bool IsVALocked { get; set; }
}
