namespace UBIS.Services.UserProfile.Domain.Entities;

/// <summary>Derived from UserIpRequest.ApproveFlag — see ApproveFlagValues for the real column mapping.</summary>
public enum IpRequestStatus
{
    Pending,
    Approved,
    NotApproved
}
