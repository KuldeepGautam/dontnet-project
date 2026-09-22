namespace UBIS.Web.Services.Logging;

public interface IWebLogClient
{
    Task InfoAsync(string message, object? properties = null, CancellationToken ct = default);

    Task WarnAsync(string message, object? properties = null, CancellationToken ct = default);

    Task ErrorAsync(string message, Exception? exception = null, object? properties = null, CancellationToken ct = default);
}
