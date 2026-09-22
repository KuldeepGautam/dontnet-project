namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Last-N password history (reuse-rejection check). No legacy equivalent exists — this is a new
/// table, <c>dbo.M_PasswordHistory</c> (int identity, DBA-approved via
/// <c>db-scripts/M_PasswordHistory.Table.sql</c>), int-keyed to match <see cref="User.UserId"/>.
/// Added 2026-07-13 (replaces the earlier Guid-keyed <c>aim.PasswordHistory</c>).
/// </summary>
public class PasswordHistory
{
    public int PasswordHistoryId { get; set; }

    public int UserId { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Standard audit columns.
    public bool? IsActive { get; set; } = true;
    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }

    // Navigation
    public User? User { get; set; }
}
