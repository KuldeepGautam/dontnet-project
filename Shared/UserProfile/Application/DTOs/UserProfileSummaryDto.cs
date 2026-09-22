namespace UBIS.Services.UserProfile.Application.DTOs;

using UBIS.Services.UserProfile.Domain.Entities;

/// <summary>Everything the UserProfile page needs in one response.</summary>
public class UserProfileSummaryDto
{
    public string? UserName { get; set; }

    public string? RoleName { get; set; }

    public DateTime? LastLoginDate { get; set; }

    /// <summary>Currently-approved allowed IPs on file at AIM — used to pre-fill the request form.</summary>
    public string? AllowedIpAddressOne { get; set; }

    public string? AllowedIpAddressTwo { get; set; }

    public string? Email { get; set; }

    /// <summary>Masked (e.g. "xxxxxx3210"), never the real number. Renamed from plaintext
    /// <c>Mobile</c> 2026-08-17 (AIM Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    /// <summary>Null if the user has never raised an IP change request.</summary>
    public IpChangeRequestStatusDto? LatestRequest { get; set; }
}

/// <summary>Direct-save Email/Mobile update — see UserLookupController.UpdateContact in AIM (no admin approval, unlike the IP-request flow).</summary>
public class UpdateContactRequestDto
{
    public string? Email { get; set; }

    public string? Mobile { get; set; }
}

public class IpChangeRequestStatusDto
{
    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    public IpRequestStatus Status { get; set; }
}

/// <summary>ExistingIp is captured by UBIS_Web from its own inbound request, not trusted from anywhere else.</summary>
public class RaiseIpChangeRequestDto
{
    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public string ExistingIp { get; set; } = string.Empty;
}

/// <summary>
/// One row of the user's IP change-request history grid on the merged User Profile page (client
/// requirement 2026-09-03: "a grid showing IP request number, list of IP addresses and date of
/// request raised, Status"). RequestNumber is the legacy table's own RowId (already a stable,
/// unique identifier — no separate numbering scheme needed). Status is deliberately collapsed to
/// just two values here (unlike IpChangeRequestStatusDto.Status's 3-way Pending/Approved/
/// NotApproved) per the client's own wording: "Status can be completed or Active" — Pending maps
/// to Active (still awaiting a decision), Approved/NotApproved both map to Completed (a decision
/// was made, nothing left pending on this request).
/// </summary>
public class IpRequestHistoryItemDto
{
    public int RequestNumber { get; set; }

    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public DateTime? RequestDate { get; set; }

    public string Status { get; set; } = "Active";
}
