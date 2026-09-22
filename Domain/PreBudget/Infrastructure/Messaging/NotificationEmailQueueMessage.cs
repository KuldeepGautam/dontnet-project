namespace UBIS.Services.PreBudget.Infrastructure.Messaging;

/// <summary>Wire shape published onto Core/Email's "email.notification.send" routing key - field-for-field copy of Email's own NotificationEmailQueueMessage (the two ends of a queue message must agree on shape; this solution has no shared contracts library). Added 2026-08-14.</summary>
public class NotificationEmailQueueMessage
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
}
