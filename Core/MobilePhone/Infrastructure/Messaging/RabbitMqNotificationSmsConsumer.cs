using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UBIS.Services.MobilePhone.Application.DTOs;
using UBIS.Services.MobilePhone.Application.Interfaces;
using UBIS.Services.MobilePhone.Infrastructure.Logging;

namespace UBIS.Services.MobilePhone.Infrastructure.Messaging;

/// <summary>
/// Mirrors Email's RabbitMqOtpEmailConsumer/RabbitMqNotificationEmailConsumer shape: binds a
/// durable queue to the "mobile" topic exchange with pattern "mobile.notification.#" and hands
/// each message to <see cref="ISmsSender"/>. Fire-and-forget from the publisher's perspective:
/// success/failure is recorded to SmsDispatchLog and LogWriter, never reported back over the queue.
/// </summary>
public class RabbitMqNotificationSmsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogWriterClient _logWriter;
    private readonly ILogger<RabbitMqNotificationSmsConsumer> _logger;

    public RabbitMqNotificationSmsConsumer(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogWriterClient logWriter,
        ILogger<RabbitMqNotificationSmsConsumer> logger)
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
                exchange: _options.MobileExchange,
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
                exchange: _options.MobileExchange,
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

            _logger.LogInformation("Notification SMS consumer listening on queue {Queue}.", _options.NotificationQueue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification SMS consumer failed to start on queue {Queue}.", _options.NotificationQueue);
            await _logWriter.ErrorAsync("MobilePhoneService notification consumer failed to start.", ex, new { _options.NotificationQueue }, CancellationToken.None);
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
            var queueMessage = JsonSerializer.Deserialize<SmsQueueMessage>(json, JsonOptions);

            if (queueMessage == null)
            {
                _logger.LogWarning("Received an unparsable notification SMS message; dropping without retry.");
                await _logWriter.WarnAsync("Received an unparsable notification SMS message; dropped.", ct: stoppingToken);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                return;
            }

            messageId = queueMessage.MessageId;

            using var scope = _scopeFactory.CreateScope();
            var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();

            var request = new SendNotificationSmsRequest
            {
                MessageId = queueMessage.MessageId,
                ToMobile = queueMessage.ToMobile,
                Message = queueMessage.Message,
                Purpose = queueMessage.Purpose,
                RequestedAtUtc = queueMessage.RequestedAtUtc
            };

            // Sent/Skipped/Failed is already recorded to SmsDispatchLog and LogWriter by the
            // sender; ack either way so a persistently-failing gateway cannot wedge the queue.
            await smsSender.SendNotificationSmsAsync(request, stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing notification SMS message {MessageId}; requeueing.", messageId);
            await _logWriter.ErrorAsync("Unexpected error processing notification SMS message.", ex, new { MessageId = messageId }, stoppingToken);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}
