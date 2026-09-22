using UBIS.Services.Email.Application.DTOs;

namespace UBIS.Services.Email.Application.Interfaces;

public interface IEmailSender
{
    Task<Result> SendOtpEmailAsync(SendOtpEmailRequest request, CancellationToken ct);

    Task<Result> SendNotificationEmailAsync(SendNotificationEmailRequest request, CancellationToken ct);
}
