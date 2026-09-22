namespace UBIS.Services.LogWriter.Services;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UBIS.Services.LogWriter.Domain;
using UBIS.Services.LogWriter.Persistence;

/// <summary>
/// Binds a fresh, anonymous, non-durable queue to the "log" topic exchange with pattern "log.*"
/// (matches every other service's LogEntryMessage publisher — see RabbitMq:LogRoutingKeyPrefix in
/// their configs) and persists each entry to dbo.M_LogEntry. Anonymous/non-durable is intentional:
/// this is a best-effort sink, not a guaranteed-delivery queue — messages published while
/// LogWriter is down are dropped rather than buffered, since nothing is bound to the exchange to
/// catch them.
///
/// Previously implemented via reflection against RabbitMQ.Client's old synchronous API
/// (ConnectionFactory.CreateConnection/IModel.CreateModel), which was removed in the referenced
/// v7.2.1 package (async-only: CreateConnectionAsync/CreateChannelAsync) — that made this service
/// crash with a NullReferenceException on every single startup (GetMethod("CreateConnection")
/// returned null, then Invoke on null threw). Rewritten to use the real typed async API directly,
/// same shape as Core/Email/Infrastructure/Messaging/RabbitMqOtpEmailConsumer.cs.
/// </summary>
public class RabbitMqListenerService : BackgroundService
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqListenerService> _logger;

    public RabbitMqListenerService(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqListenerService> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel? channel = null;
        try
        {
            channel = await _connectionProvider.CreateChannelAsync(stoppingToken);
            await channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: "topic",
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var queueDeclareResult = await channel.QueueDeclareAsync(cancellationToken: stoppingToken);
            var queueName = queueDeclareResult.QueueName;

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKeyPattern,
                cancellationToken: stoppingToken);
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => OnMessageReceivedAsync(channel, ea, stoppingToken);

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("LogWriter RabbitMQ consumer listening on queue {Queue}, exchange {Exchange}.", queueName, _options.Exchange);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogWriter RabbitMQ consumer failed to start.");
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
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var incoming = JsonSerializer.Deserialize<IncomingLogEntryMessage>(json);
            if (incoming == null)
            {
                _logger.LogWarning("Received an unparsable log message; dropping without retry.");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                return;
            }

            var entry = new LogEntry
            {
                Level = incoming.Level,
                Message = incoming.Message,
                Exception = incoming.Exception,
                Properties = incoming.Properties,
                CreatedAt = DateTime.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
            db.Logs.Add(entry);
            await db.SaveChangesAsync(stoppingToken);

            var levelEnum = ParseLevel(entry.Level);
            var exObj = string.IsNullOrWhiteSpace(entry.Exception) ? null : new Exception(entry.Exception);
            Serilog.Log.ForContext("Source", "RabbitMQ").Write(levelEnum, exObj, "{Message} {Properties}", entry.Message, entry.Properties);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log message; requeueing.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }

    private static Serilog.Events.LogEventLevel ParseLevel(string level)
    {
        if (Enum.TryParse<Serilog.Events.LogEventLevel>(level, true, out var lv)) return lv;
        return Serilog.Events.LogEventLevel.Information;
    }
}
