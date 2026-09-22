namespace UBIS.Services.MobilePhone.Infrastructure.Logging;

/// <summary>
/// Ships log/warning/error events to the LogWriter microservice. Callers must never
/// pass a full (unmasked) mobile number in message or properties.
/// </summary>
public interface ILogWriterClient
{
    Task InfoAsync(string message, object? properties = null, CancellationToken ct = default);

    Task WarnAsync(string message, object? properties = null, CancellationToken ct = default);

    Task ErrorAsync(string message, Exception? exception = null, object? properties = null, CancellationToken ct = default);
}
