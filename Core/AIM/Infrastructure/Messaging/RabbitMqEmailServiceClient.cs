namespace UBIS.Services.Aim.Infrastructure.Messaging;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Publishes OTP send requests to the "email" topic exchange with routing key
/// "email.otp.send", matching how EmailService's RabbitMqOtpEmailConsumer declares the
/// same exchange and binds its queue with pattern "email.otp.#". Using a topic exchange
/// (rather than the default exchange + fixed queue name) lets future email message types
/// (e.g. "email.notification.send") share the same exchange without touching this binding.
/// Fire-and-forget: publish confirmation to the broker is the only acknowledgement AIM waits for.
/// </summary>
public class RabbitMqEmailServiceClient : IEmailServiceClient
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;

    public RabbitMqEmailServiceClient(RabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public async Task SendOtpEmailAsync(OtpEmailDispatchRequest request, CancellationToken ct = default)
    {
        await using var channel = await _connectionProvider.CreateChannelAsync(ct).ConfigureAwait(false);
        await channel.ExchangeDeclareAsync(
            exchange: _options.EmailExchange,
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: ct).ConfigureAwait(false);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

        await channel.BasicPublishAsync(
                exchange: _options.EmailExchange,
                routingKey: _options.OtpRoutingKey,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body,
                cancellationToken: ct)
            .ConfigureAwait(false);
    }
}
