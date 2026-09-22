namespace UBIS.Services.PreBudget.Application.DTOs;

/// <summary>Email/SMS opt-in for one recipient category on the Allocation screen. Added 2026-08-14.</summary>
public class NotificationChannelFlags
{
    public bool Email { get; set; }
    public bool Sms { get; set; }
}

/// <summary>
/// "Add Allocation" submit payload — bulk-assigns a TargetDate to (DemandId × AppendixId) pairs,
/// opening those appendix entry screens for those Demands until the date passes (see
/// AppendixBaseViewModel.IsEntryLocked in UBIS_Web). AppendixId null means "All" - every active
/// appendix for the financial year. Added 2026-08-14.
/// </summary>
public class AllocateAppendixDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
    public int? AppendixId { get; set; }
    public List<int> DemandIds { get; set; } = new();

    public NotificationChannelFlags NotifyDemandBudgetOfficer { get; set; } = new();
    public NotificationChannelFlags NotifyDemandCca { get; set; } = new();
    public NotificationChannelFlags NotifyDemandFa { get; set; } = new();
    public NotificationChannelFlags NotifyBudgetDivisionOfficer { get; set; } = new();
    public NotificationChannelFlags NotifySectionUser { get; set; } = new();
}

public class AllocationResultDto
{
    public int DemandsAllocated { get; set; }
    public int AppendixesAllocated { get; set; }

    /// <summary>DemandIds skipped because that (Demand, Appendix) pair was already frozen - can't re-allocate a frozen submission.</summary>
    public List<int> SkippedFrozenDemandIds { get; set; } = new();
}

/// <summary>Mirrors AIM's ContactByRoleDto (GET api/users/contacts-by-role) - the response shape a server-to-server call gets back. Added 2026-08-14.</summary>
public class RecipientContactDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>One row of the existing-allocations grid for a selected Appendix. Added 2026-08-14.</summary>
public class AllocationListItemDto
{
    public int DemandId { get; set; }
    public int DemandNo { get; set; }

    /// <summary>Added 2026-08-28 (client requirement: grid should show "Demand Name" - format
    /// "{DemandNo} - {DemandName}" - instead of the bare Demand No).</summary>
    public string DemandName { get; set; } = string.Empty;

    public DateTime? TargetDate { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
}

/// <summary>
/// Row-level edit for the existing-allocations grid (client requirement 2026-08-28: an Edit
/// button per row lets the ABO-DS-Director/Section User correct TargetDate/Frozen/NilSubmitted for
/// one Demand directly, instead of only ever bulk-reallocating). AppendixId null means "every
/// active appendix for the financial year", matching AllocateAppendixDto/GetAllocationsAsync's own
/// "All" convention - applies the same TargetDate/Frozen/NilSubmitted values to every
/// DemandAllocation row in that set for this Demand, since the grid's own row is itself an
/// aggregate across that same set (see AppendixService.GetAllocationsAsync).
/// </summary>
public class UpdateAllocationDto
{
    public int DemandId { get; set; }
    public int? AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
}
