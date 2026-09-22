namespace UBIS.Web.Services.Logging;

/// <summary>Wire shape the LogWriter microservice's RabbitMQ listener expects.</summary>
internal sealed class LogEntryMessage
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTime CreatedAt { get; set; }
}
