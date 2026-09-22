using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace UBIS.Services.MobilePhone.Infrastructure.Messaging;

/// <summary>
/// Lazily creates and shares a single RabbitMQ connection for the process,
/// since publishers (LogWriter client) and the notification-SMS consumer can each
/// open their own channel on top of it.
/// </summary>
public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct).ConfigureAwait(false);
        return await connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                // Explicit short timeout so a missing/not-yet-started RabbitMQ broker fails fast
                // instead of blocking on the library's own default connect timeout.
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
