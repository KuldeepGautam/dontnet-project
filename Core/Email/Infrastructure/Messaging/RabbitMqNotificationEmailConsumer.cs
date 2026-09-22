using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UBIS.Services.Email.Application.DTOs;
using UBIS.Services.Email.Application.Interfaces;
using UBIS.Services.Email.Infrastructure.Logging;

namespace UBIS.Services.Email.Infrastructure.Messaging;

/// <summary>
/// Sibling to <see cref="RabbitMqOtpEmailConsumer"/> — binds its own durable queue to the same
/// "email" topic exchange with pattern "email.notification.#", the extension point that class's
/// doc comment anticipated. Kept as a separate class (rather than folded into the OTP consumer)
/// since the two message shapes and dispatch-log semantics differ enough to make one class messy.
/// Fire-and-forget from the publisher's perspective: success/failure is recorded to
/// EmailDispatchLog and LogWriter, never reported back over the queue.
/// </summary>
public class RabbitMqNotificationEmailConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogWriterClient _logWriter;
    private readonly ILogger<RabbitMqNotificationEmailConsumer> _logger;

    public RabbitMqNotificationEmailConsumer(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogWriterClient logWriter,
        ILogger<RabbitMqNotificationEmailConsumer> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logWriter = logWriter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel? channel = null;
        try
        {
            channel = await _connectionProvider.CreateChannelAsync(stoppingToken);
            await channel.ExchangeDeclareAsync(
                exchange: _options.EmailExchange,
                type: "topic",
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(
                queue: _options.NotificationQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(
                queue: _options.NotificationQueue,
                exchange: _options.EmailExchange,
                routingKey: _options.NotificationBindingPattern,
                cancellationToken: stoppingToken);
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => OnMessageReceivedAsync(channel, ea, stoppingToken);

            await channel.BasicConsumeAsync(
                queue: _options.NotificationQueue,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Notification email consumer listening on queue {Queue}.", _options.NotificationQueue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification email consumer failed to start on queue {Queue}.", _options.NotificationQueue);
            await _logWriter.ErrorAsync("EmailService notification consumer failed to start.", ex, new { _options.NotificationQueue }, CancellationToken.None);
        }
        finally
        {
            if (channel != null)
            {
                await channel.DisposeAsync();
            }
        }
    }

    private async Task OnMessageReceivedAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        Guid? messageId = null;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var queueMessage = JsonSerializer.Deserialize<NotificationEmailQueueMessage>(json, JsonOptions);

            if (queueMessage == null)
            {
                _logger.LogWarning("Received an unparsable notification email message; dropping without retry.");
                await _logWriter.WarnAsync("Received an unparsable notification email message; dropped.", ct: stoppingToken);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                return;
            }

            messageId = queueMessage.MessageId;

            using var scope = _scopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var request = new SendNotificationEmailRequest
            {
                MessageId = queueMessage.MessageId,
                ToEmail = queueMessage.ToEmail,
                ToName = queueMessage.ToName,
                Subject = queueMessage.Subject,
                Body = queueMessage.Body,
                RequestedAtUtc = queueMessage.RequestedAtUtc
            };

            // Sent/Failed is already recorded to EmailDispatchLog and LogWriter by the sender;
            // ack either way so a persistently-failing SMTP host cannot wedge the queue.
            await emailSender.SendNotificationEmailAsync(request, stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing notification email message {MessageId}; requeueing.", messageId);
            await _logWriter.ErrorAsync("Unexpected error processing notification email message.", ex, new { MessageId = messageId }, stoppingToken);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}
