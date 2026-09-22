namespace UBIS.Services.Aim.Infrastructure.Sms;

using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Placeholder <see cref="ISmsServiceClient"/> registered until a real intranet SMS gateway is
/// available (client is pursuing access via NIC HQ). Fails gracefully — never throws — so
/// flipping <c>Mfa:Channel</c> to "Sms" before that happens surfaces a clear, actionable error
/// instead of crashing the login flow.
/// </summary>
public class NotConfiguredSmsServiceClient : ISmsServiceClient
{
    private readonly ILogWriterClient _logWriter;

    public NotConfiguredSmsServiceClient(ILogWriterClient logWriter)
    {
        _logWriter = logWriter;
    }

    public async Task<Result<bool>> SendOtpSmsAsync(string toMobile, string otp, CancellationToken ct = default)
    {
        await _logWriter.WarnAsync(
            "SMS MFA channel selected but no SMS gateway is configured yet.",
            new { ToMobileMasked = MaskMobile(toMobile) },
            ct);

        return Result<bool>.Failure(
            "SMS_CHANNEL_NOT_CONFIGURED",
            "SMS-based verification is not yet available. Contact your administrator or switch Mfa:Channel to Email.");
    }

    private static string MaskMobile(string mobile) =>
        string.IsNullOrEmpty(mobile) || mobile.Length <= 4
            ? "****"
            : new string('*', mobile.Length - 4) + mobile[^4..];
}
