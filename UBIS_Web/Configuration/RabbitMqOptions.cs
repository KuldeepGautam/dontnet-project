namespace UBIS.Web.Configuration;

/// <summary>Binds to "RabbitMq". Same shape/exchange as AIM and MenuGenerator use for centralized logging.</summary>
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
