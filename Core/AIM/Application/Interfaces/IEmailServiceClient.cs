namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Application.DTOs;

/// <summary>
/// Publishes OTP send requests to the Email microservice (integration contract:
/// queue "email.otp.send"). Fire-and-forget — AIM must not block waiting on
/// EmailService's ack beyond publish-confirmation to the broker.
/// </summary>
public interface IEmailServiceClient
{
    Task SendOtpEmailAsync(OtpEmailDispatchRequest request, CancellationToken ct = default);
}
