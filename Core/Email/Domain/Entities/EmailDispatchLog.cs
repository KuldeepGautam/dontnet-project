namespace UBIS.Services.Email.Domain.Entities;

/// <summary>
/// No legacy equivalent exists — this is a new table, <c>dbo.M_EmailDispatchLog</c> (int identity,
/// DBA-approved via <c>db-scripts/M_EmailDispatchLog.Table.sql</c>). Moved from a Guid-keyed
/// <c>emailservice.EmailDispatchLog</c> table to <c>dbo</c> on 2026-07-13, per the identity-model
/// rewrite (single shared <c>BIMS2</c> database, no per-service schemas). <see cref="MessageId"/>
/// stays a Guid — it's the RabbitMQ message correlation id, not a row identity. User-confirmed
/// 2026-07-13: this is a pure email-send log, not a user-facing/audited record, so it deliberately
/// carries none of the standard 7-column audit template other new tables get.
/// </summary>
public class EmailDispatchLog
{
    public int Id { get; set; }
    public Guid MessageId { get; set; }
    public string ToEmailMasked { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderResponseCode { get; set; }
    public DateTime SentAtUtc { get; set; }
}
