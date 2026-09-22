namespace UBIS.Services.PreBudget.Infrastructure.Messaging;

/// <summary>Wire shape published onto Core/MobilePhone's "mobile.notification.send" routing key - field-for-field copy of MobilePhone's own SmsQueueMessage. Added 2026-08-14.</summary>
public class SmsQueueMessage
{
    public Guid MessageId { get; set; }
    public string ToMobile { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Notification";
    public DateTime RequestedAtUtc { get; set; }
}
