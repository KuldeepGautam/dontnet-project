namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the new <c>dbo.M_MapUserIPAddress</c> table (int identity) — extracted from
/// <c>M_User.IPadres1</c>/<c>IPadres2</c>/<c>IPAuthFlag</c> 2026-08-17, since a user's allowed
/// login IPs are a grant, not a core identity attribute. Written by
/// <c>ComplianceController.ApproveChangeRequest</c> when an admin approves a pending IP change
/// request; read at login by <c>AuthenticationService</c>'s IP-binding enforcement.
/// </summary>
public class MapUserIPAddress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? IPAddress1 { get; set; }

    public string? IPAddress2 { get; set; }

    /// <summary>"Y"/"N" — whether IP-binding enforcement is active for this user.</summary>
    public string IPAuthFlag { get; set; } = "N";

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
}
