namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>
/// Shape of the value stored at Redis key <c>ubis:session:{sessionId}</c> (added 2026-08-10,
/// replaces the earlier anonymous-type blob that only had UserId/UserName/RoleId/RoleName/
/// Permissions). Extended with LoginTimeUtc/IPAddress/Status/LastActivityUtc to back the Active
/// Session Monitor admin page and its test case (session record created at login with Login Time,
/// User, Role, IP Address, Status=Active; Last Activity updated on each heartbeat).
/// </summary>
public class SessionRecordDto
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public List<int> Permissions { get; set; } = new();

    public DateTime LoginTimeUtc { get; set; }

    public string? IPAddress { get; set; }

    /// <summary>Always "Active" while the Redis key exists - there is no "LoggedOut" value ever
    /// persisted, since Logout/expiry both delete the key outright rather than flagging it.</summary>
    public string Status { get; set; } = "Active";

    public DateTime LastActivityUtc { get; set; }
}

/// <summary>Active Session Monitor row - SessionRecordDto plus the SessionId key itself (not part
/// of the stored value) and the remaining Redis TTL, for admin display.</summary>
public class ActiveSessionDto
{
    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public DateTime LoginTimeUtc { get; set; }

    public string? IPAddress { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime LastActivityUtc { get; set; }

    public double? TimeToLiveSeconds { get; set; }
}
