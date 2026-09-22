namespace UBIS.ApiGateway.Logging;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

/// <summary>
/// Publishes log events onto the same RabbitMQ exchange/routing key LogWriter listens on —
/// identical pattern to RabbitMqLogWriterClient in Shared/UserProfile and every other service,
/// tagged "UBIS.ApiGateway" here instead. LogWriter already binds "log.*" (wildcard), so no
/// LogWriter-side change is needed for it to pick these up. Falls back to the local ILogger if
/// RabbitMQ is unreachable so a logging failure never masks or replaces the original error.
/// </summary>
public class GatewayLogWriterClient
{
    private const string ServiceTag = "UBIS.ApiGateway";

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<GatewayLogWriterClient> _localLogger;

    public GatewayLogWriterClient(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<GatewayLogWriterClient> localLogger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _localLogger = localLogger;
    }

    public Task ErrorAsync(string message, Exception? exception = null, object? properties = null, CancellationToken ct = default)
        => PublishAsync("Error", message, exception, properties, ct);

    private async Task PublishAsync(string level, string message, Exception? exception, object? properties, CancellationToken ct)
    {
        var taggedMessage = $"{ServiceTag} - {level} - {message}";

        var entry = new LogEntryMessage
        {
            Id = Guid.NewGuid(),
            Level = level,
            Message = taggedMessage,
            Exception = exception?.ToString(),
            Properties = properties == null ? null : JsonSerializer.Serialize(properties),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await using var channel = await _connectionProvider.CreateChannelAsync(ct).ConfigureAwait(false);
            await channel.ExchangeDeclareAsync(_options.LogExchange, type: "topic", durable: true, autoDelete: false, cancellationToken: ct).ConfigureAwait(false);

            var routingKey = $"{_options.LogRoutingKeyPrefix}.{level.ToLowerInvariant()}";
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry));
            await channel.BasicPublishAsync(_options.LogExchange, routingKey, mandatory: false, basicProperties: new BasicProperties(), body: body, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception publishEx)
        {
            _localLogger.LogError(exception, "{Message}", taggedMessage);
            _localLogger.LogWarning(publishEx, "Failed to publish log entry to LogWriter via RabbitMQ; wrote locally instead.");
        }
    }
}
