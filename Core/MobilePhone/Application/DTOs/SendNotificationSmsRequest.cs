namespace UBIS.Services.MobilePhone.Application.DTOs;

public class SendNotificationSmsRequest
{
    public Guid MessageId { get; set; }
    public string ToMobile { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Notification";
    public DateTime RequestedAtUtc { get; set; }
}
