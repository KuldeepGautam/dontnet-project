namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>
/// "Add Allocation" screen (FunctionId 528) - lets ABO-DS-Director/Section User bulk-assign a
/// Target Date to (Demand x Appendix) pairs, opening those appendix entry screens until the date
/// passes (see AppendixBaseViewModel.IsEntryLocked). UX deliberately minimal here - a designer
/// replaces this view later; this model exists to drive the full API surface. Added 2026-08-14.
/// </summary>
public class AllocationViewModel
{
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Null = "All" (the default) - allocates every active appendix for the year.</summary>
    public int? SelectedAppendixId { get; set; }

    public List<PreBudgetAppendixDto> Appendixes { get; set; } = new();

    public List<DemandDto> Demands { get; set; } = new();

    /// <summary>Already sorted by DemandNo and sliced down to just the current page (client
    /// requirement 2026-08-28: "add 15 record pagination to grid, sort by DemandSrNo") - see
    /// TotalAllocationCount/Page/PageSize/TotalPages for the pager UI.</summary>
    public List<AllocationListItemDto> ExistingAllocations { get; set; } = new();

    /// <summary>Total rows before paging - distinct from ExistingAllocations.Count, which is just this page's slice.</summary>
    public int TotalAllocationCount { get; set; }

    public int Page { get; set; } = 1;

    public const int PageSize = 15;

    public int TotalPages => TotalAllocationCount == 0 ? 1 : (int)Math.Ceiling(TotalAllocationCount / (double)PageSize);

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}

/// <summary>Form-post shape for the Submit button - mirrors AllocateAppendixDto's fields as plain MVC-bindable properties. Added 2026-08-14.</summary>
public class AllocationSubmitRequest
{
    public int? AppendixId { get; set; }

    public List<int> DemandIds { get; set; } = new();

    public DateTime TargetDate { get; set; }

    public bool NotifyDemandBudgetOfficerEmail { get; set; }
    public bool NotifyDemandBudgetOfficerSms { get; set; }
    public bool NotifyDemandCcaEmail { get; set; }
    public bool NotifyDemandCcaSms { get; set; }
    public bool NotifyDemandFaEmail { get; set; }
    public bool NotifyDemandFaSms { get; set; }
    public bool NotifyBudgetDivisionOfficerEmail { get; set; }
    public bool NotifyBudgetDivisionOfficerSms { get; set; }
    public bool NotifySectionUserEmail { get; set; }
    public bool NotifySectionUserSms { get; set; }
}
