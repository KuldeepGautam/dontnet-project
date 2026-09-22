namespace UBIS.Services.Email.Application.DTOs;

public class SendOtpEmailRequest
{
    public Guid MessageId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Otp { get; set; } = string.Empty;
    public string Purpose { get; set; } = "LoginMfa";
    public int ExpiryMinutes { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
