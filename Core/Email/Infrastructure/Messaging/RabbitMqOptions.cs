namespace UBIS.Services.Email.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Topic exchange every email-type message (OTP today, notifications later) is published to.</summary>
    public string EmailExchange { get; set; } = "email";

    /// <summary>Durable queue this consumer binds to <see cref="EmailExchange"/>.</summary>
    public string Queue { get; set; } = "email.otp.send.queue";

    /// <summary>Binding pattern for <see cref="Queue"/> — matches "email.otp.send" today and any future "email.otp.*" key.</summary>
    public string OtpBindingPattern { get; set; } = "email.otp.#";

    /// <summary>Durable queue the notification-email consumer binds to <see cref="EmailExchange"/> — separate from <see cref="Queue"/> so OTP and notification traffic never share one queue.</summary>
    public string NotificationQueue { get; set; } = "email.notification.send.queue";

    /// <summary>Binding pattern for <see cref="NotificationQueue"/> — matches "email.notification.send" and any future "email.notification.*" key.</summary>
    public string NotificationBindingPattern { get; set; } = "email.notification.#";

    public string LogExchange { get; set; } = "log";

    /// <summary>Prefix combined with the log level to form the routing key, e.g. "log.error". LogWriter binds with pattern "{prefix}.*".</summary>
    public string LogRoutingKeyPrefix { get; set; } = "log";
}
