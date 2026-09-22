namespace UBIS.Services.Email.Application.DTOs;

public class SendOtpEmailResult
{
    public Guid MessageId { get; init; }
    public bool Accepted { get; init; }
}
