namespace UBIS.ApiGateway.Logging;

/// <summary>Wire shape LogWriter's RabbitMQ listener expects — same as every other service's copy.</summary>
internal sealed class LogEntryMessage
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTime CreatedAt { get; set; }
}
