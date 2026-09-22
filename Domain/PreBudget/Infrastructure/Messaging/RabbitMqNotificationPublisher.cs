namespace UBIS.Services.PreBudget.Infrastructure.Messaging;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>
/// Publishes Allocation-screen recipient notifications onto Core/Email's and Core/MobilePhone's
/// queues, matching how AIM's RabbitMqEmailServiceClient publishes OTP emails onto the same
/// exchange Email consumes from. A broker/publish failure is logged and swallowed here rather than
/// propagated - never let a notification failure block the allocate response the caller is waiting
/// on (same "fire-and-forget from the caller's perspective" contract Email's own OTP consumer
/// documents). Added 2026-08-14.
/// </summary>
public class RabbitMqNotificationPublisher : INotificationPublisher
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqNotificationPublisher> _logger;

    public RabbitMqNotificationPublisher(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqNotificationPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishEmailAsync(string toEmail, string? toName, string subject, string body, CancellationToken ct = default)
    {
        var message = new NotificationEmailQueueMessage
        {
            MessageId = Guid.NewGuid(),
            ToEmail = toEmail,
            ToName = toName,
            Subject = subject,
            Body = body,
            RequestedAtUtc = DateTime.UtcNow
        };

        await PublishAsync(_options.EmailExchange, _options.EmailNotificationRoutingKey, message, ct);
    }

    public async Task PublishSmsAsync(string toMobile, string message, CancellationToken ct = default)
    {
        var queueMessage = new SmsQueueMessage
        {
            MessageId = Guid.NewGuid(),
            ToMobile = toMobile,
            Message = message,
            RequestedAtUtc = DateTime.UtcNow
        };

        await PublishAsync(_options.MobileExchange, _options.SmsNotificationRoutingKey, queueMessage, ct);
    }

    private async Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, CancellationToken ct)
    {
        try
        {
            await using var channel = await _connectionProvider.CreateChannelAsync(ct).ConfigureAwait(false);
            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: "topic",
                durable: true,
                autoDelete: false,
                cancellationToken: ct).ConfigureAwait(false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: new BasicProperties(),
                    body: body,
                    cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish notification message to {Exchange}/{RoutingKey}; recipient will not be notified.", exchange, routingKey);
        }
    }
}
