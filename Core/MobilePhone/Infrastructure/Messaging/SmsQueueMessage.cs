namespace UBIS.Services.MobilePhone.Infrastructure.Messaging;

/// <summary>
/// Wire shape published onto the "mobile.notification.send" queue by any service that needs a
/// plain SMS notification delivered — e.g. PreBudget's allocation-submit flow. No templating
/// engine: Message is sent as-is.
/// </summary>
public class SmsQueueMessage
{
    public Guid MessageId { get; set; }
    public string ToMobile { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Notification";
    public DateTime RequestedAtUtc { get; set; }
}
