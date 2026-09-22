namespace UBIS.Services.Aim.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Topic exchange every email-type message (OTP today, notifications later) publishes to.</summary>
    public string EmailExchange { get; set; } = "email";

    /// <summary>Durable queue Email's consumer binds to <see cref="EmailExchange"/> for OTP messages.</summary>
    public string OtpQueue { get; set; } = "email.otp.send.queue";

    /// <summary>Routing key published for OTP send requests; Email's consumer binds with pattern "email.otp.#".</summary>
    public string OtpRoutingKey { get; set; } = "email.otp.send";

    /// <summary>
    /// Routing key for Section-4 profile/IP-change notifications (published only when
    /// EnableEmailFeatures is true) — shares the "email" exchange under a distinct key so
    /// Email's consumer can bind a separate queue for it later without touching the OTP binding.
    /// </summary>
    public string NotificationRoutingKey { get; set; } = "email.notification.send";

    public string LogExchange { get; set; } = "log";

    /// <summary>Prefix combined with the log level to form the routing key, e.g. "log.error". LogWriter binds with pattern "{prefix}.*".</summary>
    public string LogRoutingKeyPrefix { get; set; } = "log";
}
