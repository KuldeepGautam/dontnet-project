namespace UBIS.Services.PreBudget.Infrastructure.Messaging;

/// <summary>
/// Publish-only RabbitMQ config — PreBudget never consumes, only publishes Allocation-screen
/// notification messages onto queues Email/MobilePhone already own and bind to. Mirrors AIM's own
/// RabbitMqOptions publish-side shape. Added 2026-08-14.
/// </summary>
public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Topic exchange Core/Email's RabbitMqNotificationEmailConsumer binds to with pattern "email.notification.#".</summary>
    public string EmailExchange { get; set; } = "email";

    /// <summary>Routing key for a generic notification email (as opposed to AIM's OTP-only "email.otp.send").</summary>
    public string EmailNotificationRoutingKey { get; set; } = "email.notification.send";

    /// <summary>Topic exchange Core/MobilePhone's RabbitMqNotificationSmsConsumer binds to with pattern "mobile.notification.#".</summary>
    public string MobileExchange { get; set; } = "mobile";

    public string SmsNotificationRoutingKey { get; set; } = "mobile.notification.send";
}
