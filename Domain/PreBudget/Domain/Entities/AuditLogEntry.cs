namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to the new dbo.PreBudgetAuditLog table — single generic audit-history table replacing the
/// old app's 21 per-appendix "_log" tables (design decision, confirmed 2026-07-23). Populated by
/// PreBudgetDbContext.SaveChangesAsync's override (BuildAuditEntries) rather than a separate
/// SaveChangesInterceptor class or per-entity code — same effect, one less moving part. Covers
/// every tracked entity type, not just appendices.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }

    public string TableName { get; set; } = string.Empty;

    public int RecordId { get; set; }

    public string Action { get; set; } = string.Empty;

    public int? ChangedByUserId { get; set; }

    public string? ChangedByUserName { get; set; }

    public string? ChangedByIp { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }
}

public static class AuditAction
{
    public const string Insert = "Insert";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Freeze = "Freeze";
}
