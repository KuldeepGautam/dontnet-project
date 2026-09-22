namespace UBIS.Services.Email.Infrastructure.Logging;

/// <summary>
/// Ships log/warning/error events to the LogWriter microservice. Callers must never
/// pass the raw OTP or a full (unmasked) email address in message or properties.
/// </summary>
public interface ILogWriterClient
{
    Task InfoAsync(string message, object? properties = null, CancellationToken ct = default);

    Task WarnAsync(string message, object? properties = null, CancellationToken ct = default);

    Task ErrorAsync(string message, Exception? exception = null, object? properties = null, CancellationToken ct = default);
}
