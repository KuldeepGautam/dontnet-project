namespace UBIS.Services.Email.Infrastructure.Messaging;

/// <summary>
/// Wire shape published onto the "email.notification.send" queue by any service that needs a
/// plain (non-OTP) notification email delivered — e.g. PreBudget's allocation-submit flow. No
/// templating engine: Subject/Body are sent as-is.
/// </summary>
public class NotificationEmailQueueMessage
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
}
