namespace UBIS.Services.Aim.Application.DTOs;

/// <summary>
/// Wire shape published onto the Email microservice's "email.otp.send" queue.
/// </summary>
public class OtpEmailDispatchRequest
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Otp { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
