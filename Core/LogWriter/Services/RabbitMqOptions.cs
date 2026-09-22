namespace UBIS.Services.LogWriter.Services;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Topic exchange every service publishes log.{level} messages to.</summary>
    public string Exchange { get; set; } = "log";

    /// <summary>Wildcard binding pattern — matches "log.information"/"log.warning"/"log.error" from every publisher.</summary>
    public string RoutingKeyPattern { get; set; } = "log.*";
}
