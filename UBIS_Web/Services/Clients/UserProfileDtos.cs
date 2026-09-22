namespace UBIS.Web.Services.Clients;

// Local copies of UserProfile microservice's response shapes — same "no shared contracts
// project" convention as AimDtos.cs.

public enum IpRequestStatus
{
    Pending,
    Approved,
    NotApproved
}

public class IpChangeRequestStatusDto
{
    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    public IpRequestStatus Status { get; set; }
}

public class UserProfileSummaryDto
{
    public string? UserName { get; set; }

    public string? RoleName { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? AllowedIpAddressOne { get; set; }

    public string? AllowedIpAddressTwo { get; set; }

    public string? Email { get; set; }

    /// <summary>Masked (e.g. "xxxxxx3210"), never the real number. Renamed from plaintext
    /// <c>Mobile</c> 2026-08-17 (AIM Workstream 7: Mobile encryption + masking).</summary>
    public string? MaskedMobile { get; set; }

    public IpChangeRequestStatusDto? LatestRequest { get; set; }
}

/// <summary>ExistingIp is filled in by UserController/UserProfileController from its own inbound request, never client-supplied.</summary>
public class RaiseIpChangeRequestDto
{
    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public string ExistingIp { get; set; } = string.Empty;
}

public class UpdateContactRequestDto
{
    public string? Email { get; set; }

    public string? Mobile { get; set; }
}

/// <summary>Mirrors AIM's UserProfile IpRequestHistoryItemDto — one row of the merged User
/// Profile page's IP-request grid. Status is "Completed" or "Active" only.</summary>
public class IpRequestHistoryItemDto
{
    public int RequestNumber { get; set; }

    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    public DateTime? RequestDate { get; set; }

    public string Status { get; set; } = "Active";
}
