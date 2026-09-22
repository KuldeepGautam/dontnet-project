namespace UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>Fire-and-forget publisher for the Allocation screen's recipient notifications - success is only "the broker accepted it", never a delivery guarantee. Added 2026-08-14.</summary>
public interface INotificationPublisher
{
    Task PublishEmailAsync(string toEmail, string? toName, string subject, string body, CancellationToken ct = default);

    Task PublishSmsAsync(string toMobile, string message, CancellationToken ct = default);
}
