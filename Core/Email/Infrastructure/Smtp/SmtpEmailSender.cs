using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UBIS.Services.Email.Application.DTOs;
using UBIS.Services.Email.Application.Interfaces;
using UBIS.Services.Email.Domain.Entities;
using UBIS.Services.Email.Infrastructure.Configuration;
using UBIS.Services.Email.Infrastructure.Logging;
using UBIS.Services.Email.Infrastructure.Persistence;

namespace UBIS.Services.Email.Infrastructure.Smtp;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly EmailDbContext _db;
    private readonly ILogWriterClient _logWriter;
    private readonly AppSettingsReader _appSettingsReader;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        EmailDbContext db,
        ILogWriterClient logWriter,
        AppSettingsReader appSettingsReader,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _db = db;
        _logWriter = logWriter;
        _appSettingsReader = appSettingsReader;
        _logger = logger;
    }

    public async Task<Result> SendOtpEmailAsync(SendOtpEmailRequest request, CancellationToken ct)
    {
        var maskedEmail = MaskEmail(request.ToEmail);

        // dbo.AppSettings.EnableEmail (dev/offline-intranet default: off) - gates only the actual
        // SMTP dispatch below so test runs don't spam real inboxes; logging and the
        // EmailDispatchLog record still happen normally so dispatch history stays complete.
        var emailEnabled = await _appSettingsReader.GetBoolAsync("EnableEmail", ct: ct);
        if (!emailEnabled)
        {
            _logger.LogInformation("EnableEmail is off; skipping SMTP dispatch for message {MessageId} to {ToEmailMasked}", request.MessageId, maskedEmail);
            await _logWriter.InfoAsync("EnableEmail is off; OTP email send skipped.", new { request.MessageId, ToEmailMasked = maskedEmail, request.Purpose }, ct);

            await PersistDispatchLogAsync(request, maskedEmail, status: "Skipped", providerResponseCode: "000", ct);

            return Result.Ok("OTP email send skipped (EnableEmail is off).");
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host is not configured for message {MessageId}", request.MessageId);
            await _logWriter.WarnAsync("SMTP host is not configured.", new { request.MessageId, request.Purpose }, ct);
            return Result.Fail("SMTP host is not configured.");
        }

        var subject = GetSubject(request.Purpose);
        var body = BuildHtmlBody(request);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(request.ToEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            // Port 465 requires implicit TLS (SSL-on-connect); port 587 (and others) use STARTTLS -
            // System.Net.Mail.SmtpClient couldn't do the former, which is why this uses MailKit.
            var secureSocketOptions = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : (_options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, ct);
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            await PersistDispatchLogAsync(request, maskedEmail, status: "Sent", providerResponseCode: "250", ct);

            _logger.LogInformation("OTP email sent to {ToEmailMasked} for purpose {Purpose}", maskedEmail, request.Purpose);
            await _logWriter.InfoAsync("OTP email sent.", new { request.MessageId, ToEmailMasked = maskedEmail, request.Purpose }, ct);

            return Result.Ok("OTP email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email for message {MessageId} to {ToEmailMasked}", request.MessageId, maskedEmail);
            await _logWriter.ErrorAsync("Failed to send OTP email.", ex, new { request.MessageId, ToEmailMasked = maskedEmail, request.Purpose }, ct);

            await PersistDispatchLogAsync(request, maskedEmail, status: "Failed", providerResponseCode: "500", ct);

            return Result.Fail("OTP email send failed.");
        }
    }

    private async Task PersistDispatchLogAsync(SendOtpEmailRequest request, string maskedEmail, string status, string providerResponseCode, CancellationToken ct)
    {
        var log = new EmailDispatchLog
        {
            MessageId = request.MessageId,
            ToEmailMasked = maskedEmail,
            Purpose = request.Purpose,
            Status = status,
            ProviderResponseCode = providerResponseCode,
            SentAtUtc = DateTime.UtcNow
        };

        _db.EmailDispatchLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Result> SendNotificationEmailAsync(SendNotificationEmailRequest request, CancellationToken ct)
    {
        var maskedEmail = MaskEmail(request.ToEmail);

        // Same EnableEmail gate as SendOtpEmailAsync - actual SMTP dispatch is skipped when off,
        // but the EmailDispatchLog record still gets written so dispatch history stays complete.
        var emailEnabled = await _appSettingsReader.GetBoolAsync("EnableEmail", ct: ct);
        if (!emailEnabled)
        {
            _logger.LogInformation("EnableEmail is off; skipping SMTP dispatch for message {MessageId} to {ToEmailMasked}", request.MessageId, maskedEmail);
            await _logWriter.InfoAsync("EnableEmail is off; notification email send skipped.", new { request.MessageId, ToEmailMasked = maskedEmail }, ct);

            await PersistNotificationDispatchLogAsync(request, maskedEmail, status: "Skipped", providerResponseCode: "000", ct);

            return Result.Ok("Notification email send skipped (EnableEmail is off).");
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host is not configured for message {MessageId}", request.MessageId);
            await _logWriter.WarnAsync("SMTP host is not configured.", new { request.MessageId }, ct);
            return Result.Fail("SMTP host is not configured.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(request.ToEmail));
            message.Subject = request.Subject;
            message.Body = new TextPart("html") { Text = request.Body };

            var secureSocketOptions = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : (_options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, ct);
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            await PersistNotificationDispatchLogAsync(request, maskedEmail, status: "Sent", providerResponseCode: "250", ct);

            _logger.LogInformation("Notification email sent to {ToEmailMasked}", maskedEmail);
            await _logWriter.InfoAsync("Notification email sent.", new { request.MessageId, ToEmailMasked = maskedEmail }, ct);

            return Result.Ok("Notification email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification email for message {MessageId} to {ToEmailMasked}", request.MessageId, maskedEmail);
            await _logWriter.ErrorAsync("Failed to send notification email.", ex, new { request.MessageId, ToEmailMasked = maskedEmail }, ct);

            await PersistNotificationDispatchLogAsync(request, maskedEmail, status: "Failed", providerResponseCode: "500", ct);

            return Result.Fail("Notification email send failed.");
        }
    }

    private async Task PersistNotificationDispatchLogAsync(SendNotificationEmailRequest request, string maskedEmail, string status, string providerResponseCode, CancellationToken ct)
    {
        // EmailDispatchLog.Purpose is OTP-shaped (LoginMfa/PasswordSetup/...) but flexible enough
        // (plain 32-char string) to double as a category marker here rather than adding a column.
        var log = new EmailDispatchLog
        {
            MessageId = request.MessageId,
            ToEmailMasked = maskedEmail,
            Purpose = "Notification",
            Status = status,
            ProviderResponseCode = providerResponseCode,
            SentAtUtc = DateTime.UtcNow
        };

        _db.EmailDispatchLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildHtmlBody(SendOtpEmailRequest request)
    {
        return $"""
            <html>
              <body>
                <h2>Your UBIS 2.0 One-Time Password</h2>
                <p>Hello {WebUtility.HtmlEncode(request.ToName ?? "User")},</p>
                <p>Your OTP is <strong>{request.Otp}</strong>.</p>
                <p>This code expires in {request.ExpiryMinutes} minutes.</p>
                <p><strong>Do not share this OTP.</strong></p>
              </body>
            </html>
            """;
    }

    private static string GetSubject(string purpose) => purpose switch
    {
        "LoginMfa" => "Your UBIS 2.0 Login OTP",
        "PasswordSetup" => "Set Up Your New Password — OTP",
        "PasswordReset" => "Reset Your Password — OTP",
        _ => "Your UBIS 2.0 OTP"
    };

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return email;
        }

        var local = email[..1];
        var domain = at >= email.Length - 1 ? string.Empty : email[at..];
        return $"{local}***{domain}";
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = "no-reply@ubis.gov.in";
    public string FromName { get; set; } = "UBIS 2.0";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
