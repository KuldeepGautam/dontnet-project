namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>Self-service Mobile/IP change request (Section 3 Profile screen). Added 2026-07-10.</summary>
public class SubmitChangeRequestDto
{
    public string? NewMobile { get; set; }

    public string? NewIPAddressOne { get; set; }

    public string? NewIPAddressTwo { get; set; }

    public int? DemandId { get; set; }
}

/// <summary>Current pending/approved state of the caller's most recent change request. Added 2026-07-10.</summary>
public class ChangeRequestStatusDto
{
    public int? RowId { get; set; }

    public string? ApproveFlag { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    /// <summary>True exactly once per approval — the polling client uses this to trigger the 60-second kill-switch warning, then stops seeing it true again once acknowledged server-side.</summary>
    public bool JustApproved { get; set; }
}

/// <summary>One row in the admin's pending-change-requests list. Added 2026-07.</summary>
public class PendingChangeRequestDto
{
    public int RowId { get; set; }

    /// <summary>Modern <c>UserName</c> if bridged, else the legacy <c>LoginId</c>, else a placeholder.</summary>
    public string UserName { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? RequestedMobile { get; set; }

    public string? RequestedIPAddressOne { get; set; }

    public string? RequestedIPAddressTwo { get; set; }

    public DateTime RequestDate { get; set; }
}

/// <summary>Wrapper for the admin pending-change-requests list endpoint. Added 2026-07.</summary>
public class AdminChangeRequestListDto
{
    public List<PendingChangeRequestDto> Items { get; set; } = new();
}
