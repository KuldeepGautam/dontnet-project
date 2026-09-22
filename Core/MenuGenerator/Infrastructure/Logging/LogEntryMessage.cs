namespace UBIS.Services.MenuGenerator.Infrastructure.Logging;

/// <summary>
/// Wire shape expected by the LogWriter microservice's RabbitMQ listener
/// (exchange/routing key configured via RabbitMq:LogExchange / LogRoutingKey).
/// </summary>
internal sealed class LogEntryMessage
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTime CreatedAt { get; set; }
}
