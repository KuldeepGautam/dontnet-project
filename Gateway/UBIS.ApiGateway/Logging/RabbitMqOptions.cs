namespace UBIS.ApiGateway.Logging;

/// <summary>Same shape as every other service's RabbitMqOptions in this solution (e.g. Shared/UserProfile).</summary>
public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string LogExchange { get; set; } = "log";

    /// <summary>Prefix combined with the log level to form the routing key, e.g. "log.error". LogWriter binds with pattern "{prefix}.*".</summary>
    public string LogRoutingKeyPrefix { get; set; } = "log";
}
