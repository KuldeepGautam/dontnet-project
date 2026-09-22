namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

// -----------------------------------------------------------------------------------------------
// ViewModels for the 19 appendices beyond I/II/III-A. Every one follows the same shape as
// AppendixIViewModel/AppendixIIViewModel: DemandId/DemandName/FinancialYear + saved Records +
// a NewRecord bound to the entry form. Added 2026-07-23.
// -----------------------------------------------------------------------------------------------

public class AppendixBaseViewModel
{
    public int DemandId { get; set; }
    public string DemandName { get; set; } = string.Empty;

    /// <summary>Demand No parsed from the front of <see cref="DemandName"/> ("{DemandNo} - {Name}",
    /// see PreBudgetMeetingController.ResolveDemandNameAsync) - added for the history grid's "Demand
    /// No" column (2026-08-21) rather than plumbing a new field through every Build*ViewModelAsync
    /// call site. Falls back to DemandId if DemandName wasn't built in that format.</summary>
    public string DemandNo
    {
        get
        {
            var idx = DemandName.IndexOf(" - ", StringComparison.Ordinal);
            return idx > 0 ? DemandName[..idx] : DemandId.ToString();
        }
    }
    public string FinancialYear { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public bool StatusIsError { get; set; }

    /// <summary>Appendix code this view model is for (e.g. "III") - set by the controller so the shared freeze/expiry banner and Freeze button partials know which appendix to act on. Client review 2026-08-05.</summary>
    public string AppendixCode { get; set; } = string.Empty;

    /// <summary>Template-level freeze (dbo.PreBudgetSubmissionStatus.FrozenAtUtc for this Demand+Appendix+FY) - distinct from any row-level "frozen" flag an individual appendix's records may also have.</summary>
    public bool IsAppendixFrozen { get; set; }

    /// <summary>True once the Budget Division's TargetDate for this Demand+Appendix+FY has passed and it was never frozen.</summary>
    public bool IsTargetDateExpired { get; set; }

    /// <summary>AppendixId for this AppendixCode, as resolved by ApplyFreezeStatusAsync - needed by the Nil button to post back to SetNilSubmission.</summary>
    public int AppendixId { get; set; }

    /// <summary>dbo.M_Appendix.Remarks for this AppendixCode+FinancialYear, as resolved by
    /// ApplyFreezeStatusAsync (client requirement 2026-09-17, piloted on Appendix I first) - a
    /// free-text note set directly in the DB, shown read-only on the appendix's own screen.</summary>
    public string? Remarks { get; set; }

    /// <summary>dbo.M_Appendix.ParaNo for this AppendixCode+FinancialYear, as resolved by
    /// ApplyFreezeStatusAsync (client requirement 2026-09-17) - the government circular's paragraph
    /// reference (e.g. "1.2"), used only on the export ("(See Para {ParaNo})"), never shown on the
    /// on-screen entry page.</summary>
    public string? ParaNo { get; set; }

    /// <summary>Marked Nil (client meeting 2026-08-06: "Nil Button" - Temp_DemandAllocation NIL column) when there is no data to report for this Demand+Appendix+FY. Disables the entry form same as a freeze.</summary>
    public bool IsNilSubmitted { get; set; }

    /// <summary>Entry should be blocked - either frozen, past its target date, or marked Nil. Drives the disabled state of every field on the entry form.</summary>
    public bool IsEntryLocked => IsAppendixFrozen || IsTargetDateExpired || IsNilSubmitted;

    /// <summary>Client instruction 2026-09-21, Appendix IV family (IV/IV-A/IV-B): "Single demand
    /// user will see only Remarks by Ministry and Budget section can see Remarks (Budget Div)" -
    /// each role only needs to see its own note-taking column on the grid/edit drawer, not the
    /// other side's internal remarks. Set by each appendix's own Build*ViewModelAsync via
    /// PreBudgetMeetingController.ResolveRemarksColumnVisibility(session.RoleName); default true
    /// for every role/appendix that never calls that resolver, so nothing changes for them.</summary>
    public bool ShowRemarksMinistryColumn { get; set; } = true;

    public bool ShowRemarksBudgetColumn { get; set; } = true;

    // Appendix-level permission framework (VII-A/VII-B/XI/PA-ReceiptPayment). Default true so every
    // appendix that doesn't opt into permission checking (i.e. every appendix except those four)
    // behaves exactly as before - the entry form is never blocked and the alert never renders.
    public bool CanView { get; set; } = true;
    public bool CanCreate { get; set; } = true;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanSubmit { get; set; } = true;
    public bool CanApprove { get; set; } = true;

    /// <summary>True when the current role may enter data on this appendix's screen - drives the blue "you don't have permission" alert and disables the entry form when false.</summary>
    public bool HasEntryPermission => CanCreate || CanEdit;

    // "Y" = current financial year (FinancialYear itself, e.g. "2026-2027"). Several appendices
    // (Appendix IV family) label columns relative to Y, e.g. "Actual (Y-2)" = "Actual 2024-2025".
    public string PriorFinancialYear => ShiftFinancialYear(FinancialYear, -1);
    public string PriorPriorFinancialYear => ShiftFinancialYear(FinancialYear, -2);

    /// <summary>General form of <see cref="PriorFinancialYear"/>/<see cref="PriorPriorFinancialYear"/>
    /// for the handful of appendices (VI-A/VI-C's 3-trailing-year columns, etc.) that need an
    /// offset other than -1/-2 - added 2026-08-04 so every appendix view can compute its year
    /// labels from <see cref="FinancialYear"/> instead of hardcoding literal years.</summary>
    public string YearOffset(int offsetYears) => ShiftFinancialYear(FinancialYear, offsetYears);

    private static string ShiftFinancialYear(string financialYear, int offsetYears)
    {
        var parts = financialYear?.Split('-');
        if (parts == null || parts.Length != 2 || !int.TryParse(parts[0], out var startYear))
        {
            return financialYear ?? string.Empty;
        }

        var shifted = startYear + offsetYears;
        return $"{shifted}-{shifted + 1}";
    }
}
