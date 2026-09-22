namespace UBIS.Services.MobilePhone.Domain.Entities;

/// <summary>
/// SMS equivalent of Email's <c>M_EmailDispatchLog</c> — mirrors its shape exactly, including the
/// deliberate omission of the standard 7-column audit template (this is a pure send log, not a
/// user-facing/audited record). <see cref="MessageId"/> is the RabbitMQ message correlation id,
/// not a row identity.
/// </summary>
public class SmsDispatchLog
{
    public int Id { get; set; }
    public Guid MessageId { get; set; }
    public string ToMobileMasked { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderResponseCode { get; set; }
    public DateTime SentAtUtc { get; set; }
}
