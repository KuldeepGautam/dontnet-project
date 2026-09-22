namespace UBIS.Services.LogWriter.Domain;

/// <summary>
/// No legacy equivalent exists — this is a new table, <c>dbo.M_LogEntry</c> (int identity,
/// DBA-approved via <c>db-scripts/M_LogEntry.Table.sql</c>). Moved from a Guid-keyed
/// <c>logwriter.Logs</c> table to <c>dbo</c> on 2026-07-13, per the identity-model rewrite (single
/// shared <c>BIMS2</c> database, no per-service schemas).
/// </summary>
public class LogEntry
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTime CreatedAt { get; set; }

    // Standard audit columns.
    public bool? IsActive { get; set; } = true;
    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }
}
