using UBIS.Services.MobilePhone.Application.DTOs;

namespace UBIS.Services.MobilePhone.Application.Interfaces;

public interface ISmsSender
{
    Task<Result> SendNotificationSmsAsync(SendNotificationSmsRequest request, CancellationToken ct);
}
