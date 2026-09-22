namespace UBIS.Services.Aim.Infrastructure.Services;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Messaging;

/// <summary>
/// Implements the Section 4 dual-routing message toggle: reads <c>EnableEmailFeatures</c> from
/// configuration and either (a) publishes to RabbitMQ for the Email microservice, retried via a
/// Polly v8 pipeline, or (b) — when the air-gapped intranet has no SMTP path available — drops
/// RabbitMQ entirely and writes straight to LogWriter. Added 2026-07-10.
/// </summary>
public class ComplianceNotificationService(
    IConfiguration configuration,
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options,
    ILogWriterClient logWriter) : IComplianceNotificationService
{
    private readonly RabbitMqOptions _options = options.Value;

    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();

    public async Task NotifyProfileOrIpChangeAsync(int userId, string eventType, string detail, CancellationToken ct = default)
    {
        var enableEmailFeatures = configuration.GetValue<bool?>("EnableEmailFeatures") ?? true;

        if (!enableEmailFeatures)
        {
            // Air-gapped SMTP isolation: drop the message-queue path entirely, log directly instead.
            await logWriter.InfoAsync($"[ComplianceNotification:{eventType}] {detail}", new { userId }, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            await RetryPipeline.ExecuteAsync(async token =>
            {
                await using var channel = await connectionProvider.CreateChannelAsync(token).ConfigureAwait(false);
                await channel.ExchangeDeclareAsync(
                    _options.EmailExchange, type: "topic", durable: true, autoDelete: false, cancellationToken: token)
                    .ConfigureAwait(false);

                var payload = new { userId, eventType, detail, occurredAtUtc = DateTime.UtcNow };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                await channel.BasicPublishAsync(
                    _options.EmailExchange, _options.NotificationRoutingKey,
                    mandatory: false, basicProperties: new BasicProperties(), body: body, cancellationToken: token)
                    .ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // RabbitMQ unreachable even after retries — never lose the event, fall back to LogWriter.
            await logWriter.ErrorAsync(
                $"Failed to publish compliance notification '{eventType}' after retries; logged locally instead.",
                ex, new { userId }, ct).ConfigureAwait(false);
        }
    }
}
