namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Immutable security-event audit log (insert-only). No legacy equivalent exists — this is a new
/// table, <c>dbo.M_SecurityEvent</c> (int identity, DBA-approved via
/// <c>db-scripts/M_SecurityEvent.Table.sql</c>), int-keyed to match <see cref="User.UserId"/>.
/// Added 2026-07-13 (replaces the earlier Guid-keyed <c>aim.SecurityEvents</c>).
/// </summary>
public class SecurityEvent
{
    public int EventId { get; set; }

    /// <summary>Type of security event (e.g. 'LoginSuccess', 'LoginFailure', 'IpBindingFailure', 'PasswordChanged').</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Foreign key to the User (nullable for unauthenticated events).</summary>
    public int? UserId { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Detail { get; set; }

    // Standard audit columns.
    public bool? IsActive { get; set; } = true;
    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
