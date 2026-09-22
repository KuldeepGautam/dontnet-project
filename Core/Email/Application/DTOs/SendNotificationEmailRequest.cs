namespace UBIS.Services.Email.Application.DTOs;

public class SendNotificationEmailRequest
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
}
