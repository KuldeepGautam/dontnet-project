namespace UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// Section 4 "Dual Routing Message Toggle": notifies of a profile/IP-change event via whichever
/// channel <c>EnableEmailFeatures</c> selects — RabbitMQ (for the Email microservice) when true,
/// or directly to LogWriter when false (air-gapped SMTP isolation). Added 2026-07-10.
/// </summary>
public interface IComplianceNotificationService
{
    Task NotifyProfileOrIpChangeAsync(int userId, string eventType, string detail, CancellationToken ct = default);
}
