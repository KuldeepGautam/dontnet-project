namespace UBIS.Services.Email.Infrastructure.Messaging;

/// <summary>
/// Wire shape published by AIM onto the "email.otp.send" queue (integration contract §2.1).
/// </summary>
public class OtpEmailQueueMessage
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Otp { get; set; } = string.Empty;
    public string Purpose { get; set; } = "LoginMfa";
    public int ExpiryMinutes { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
