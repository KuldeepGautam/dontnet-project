namespace UBIS.Services.MenuGenerator.Application.Interfaces;

/// <summary>
/// Ships log/warning/error events to the LogWriter microservice via the same
/// RabbitMQ exchange/routing key AIM publishes to.
/// </summary>
public interface ILogWriterClient
{
    Task InfoAsync(string message, object? properties = null, CancellationToken ct = default);

    Task WarnAsync(string message, object? properties = null, CancellationToken ct = default);

    Task ErrorAsync(string message, Exception? exception = null, object? properties = null, CancellationToken ct = default);
}
