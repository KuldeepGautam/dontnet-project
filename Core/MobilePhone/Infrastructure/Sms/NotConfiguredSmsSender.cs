using Microsoft.Extensions.Logging;
using UBIS.Services.MobilePhone.Application.DTOs;
using UBIS.Services.MobilePhone.Application.Interfaces;
using UBIS.Services.MobilePhone.Domain.Entities;
using UBIS.Services.MobilePhone.Infrastructure.Configuration;
using UBIS.Services.MobilePhone.Infrastructure.Logging;
using UBIS.Services.MobilePhone.Infrastructure.Persistence;

namespace UBIS.Services.MobilePhone.Infrastructure.Sms;

/// <summary>
/// Stub <see cref="ISmsSender"/> registered until a real intranet SMS gateway is available
/// (client is pursuing access via NIC HQ — same procurement effort as AIM's
/// <c>NotConfiguredSmsServiceClient</c>, which this mirrors in spirit though not in code, since
/// that one is AIM-internal and OTP-specific). Checks <c>dbo.AppSettings.EnableSms</c> the same
/// way <c>SmtpEmailSender</c> checks <c>EnableEmail</c>: when the flag is off, OR when it's on but
/// no gateway is configured (always true right now — there is no real gateway to call), this
/// writes a "Skipped" <see cref="SmsDispatchLog"/> row and returns a success <see cref="Result"/>.
/// Never throws, never blocks the caller — flipping EnableSms on before a real gateway exists must
/// surface as a clean no-op, not a crash in the allocation-submit flow that triggered it.
/// </summary>
public class NotConfiguredSmsSender : ISmsSender
{
    private readonly MobilePhoneDbContext _db;
    private readonly ILogWriterClient _logWriter;
    private readonly AppSettingsReader _appSettingsReader;
    private readonly ILogger<NotConfiguredSmsSender> _logger;

    public NotConfiguredSmsSender(
        MobilePhoneDbContext db,
        ILogWriterClient logWriter,
        AppSettingsReader appSettingsReader,
        ILogger<NotConfiguredSmsSender> logger)
    {
        _db = db;
        _logWriter = logWriter;
        _appSettingsReader = appSettingsReader;
        _logger = logger;
    }

    public async Task<Result> SendNotificationSmsAsync(SendNotificationSmsRequest request, CancellationToken ct)
    {
        var maskedMobile = MaskMobile(request.ToMobile);

        // dbo.AppSettings.EnableSms (default: off). Even when on, there is no real SMS gateway
        // wired up yet, so dispatch is always skipped for now - only the gate check and the
        // dispatch-log record differ from the eventual real sender's behavior when off.
        var smsEnabled = await _appSettingsReader.GetBoolAsync("EnableSms", ct: ct);
        if (!smsEnabled)
        {
            _logger.LogInformation("EnableSms is off; skipping SMS dispatch for message {MessageId} to {ToMobileMasked}", request.MessageId, maskedMobile);
            await _logWriter.InfoAsync("EnableSms is off; notification SMS send skipped.", new { request.MessageId, ToMobileMasked = maskedMobile, request.Purpose }, ct);

            await PersistDispatchLogAsync(request, maskedMobile, status: "Skipped", providerResponseCode: "000", ct);

            return Result.Ok("Notification SMS send skipped (EnableSms is off).");
        }

        _logger.LogWarning("EnableSms is on but no SMS gateway is configured yet; skipping dispatch for message {MessageId} to {ToMobileMasked}", request.MessageId, maskedMobile);
        await _logWriter.WarnAsync("EnableSms is on but no SMS gateway is configured yet; notification SMS send skipped.", new { request.MessageId, ToMobileMasked = maskedMobile, request.Purpose }, ct);

        await PersistDispatchLogAsync(request, maskedMobile, status: "Skipped", providerResponseCode: "000", ct);

        return Result.Ok("Notification SMS send skipped (no SMS gateway configured yet).");
    }

    private async Task PersistDispatchLogAsync(SendNotificationSmsRequest request, string maskedMobile, string status, string providerResponseCode, CancellationToken ct)
    {
        var log = new SmsDispatchLog
        {
            MessageId = request.MessageId,
            ToMobileMasked = maskedMobile,
            Purpose = request.Purpose,
            Status = status,
            ProviderResponseCode = providerResponseCode,
            SentAtUtc = DateTime.UtcNow
        };

        _db.SmsDispatchLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    private static string MaskMobile(string mobile) =>
        string.IsNullOrEmpty(mobile) || mobile.Length <= 4
            ? "****"
            : new string('*', mobile.Length - 4) + mobile[^4..];
}
