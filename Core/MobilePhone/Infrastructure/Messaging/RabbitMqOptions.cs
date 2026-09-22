namespace UBIS.Services.MobilePhone.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Topic exchange every SMS-type message is published to. Kept separate from Email's
    /// "email" exchange - the two services have no shared traffic and no reason to share a broker
    /// resource.</summary>
    public string MobileExchange { get; set; } = "mobile";

    /// <summary>Durable queue the notification-SMS consumer binds to <see cref="MobileExchange"/>.</summary>
    public string NotificationQueue { get; set; } = "mobile.notification.send.queue";

    /// <summary>Binding pattern for <see cref="NotificationQueue"/> — matches "mobile.notification.send" and any future "mobile.notification.*" key.</summary>
    public string NotificationBindingPattern { get; set; } = "mobile.notification.#";

    public string LogExchange { get; set; } = "log";

    /// <summary>Prefix combined with the log level to form the routing key, e.g. "log.error". LogWriter binds with pattern "{prefix}.*".</summary>
    public string LogRoutingKeyPrefix { get; set; } = "log";
}
