namespace UBIS.Web.Services.Logging;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UBIS.Web.Configuration;

/// <summary>
/// Publishes log events (login success/failure, profile edits) onto the same RabbitMQ
/// exchange/routing key AIM and MenuGenerator use, so LogWriter persists everything centrally.
/// Falls back to the local ILogger if RabbitMQ is unreachable so nothing is silently dropped.
/// </summary>
public class RabbitMqWebLogClient : IWebLogClient
{
    private const string ServiceTag = "UBIS_Web";

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqWebLogClient> _localLogger;

    public RabbitMqWebLogClient(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqWebLogClient> localLogger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _localLogger = localLogger;
    }

    public Task InfoAsync(string message, object? properties = null, CancellationToken ct = default)
        => PublishAsync("Information", message, exception: null, properties, ct);

    public Task WarnAsync(string message, object? properties = null, CancellationToken ct = default)
        => PublishAsync("Warning", message, exception: null, properties, ct);

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
            LogLocally(level, taggedMessage, exception);
            _localLogger.LogWarning(publishEx, "Failed to publish log entry to LogWriter via RabbitMQ; wrote locally instead.");
        }
    }

    private void LogLocally(string level, string message, Exception? exception)
    {
        var logLevel = level switch
        {
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            _ => LogLevel.Information
        };

        _localLogger.Log(logLevel, exception, "{Message}", message);
    }
}
