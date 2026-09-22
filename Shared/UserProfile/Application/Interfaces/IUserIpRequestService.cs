namespace UBIS.Services.UserProfile.Application.Interfaces;

using UBIS.Services.UserProfile.Application.DTOs;

public interface IUserIpRequestService
{
    /// <summary>Null if the user has never raised a request.</summary>
    Task<IpChangeRequestStatusDto?> GetLatestRequestAsync(int userId, CancellationToken ct = default);

    /// <summary>Full history (newest first) for the User Profile page's IP-request grid.</summary>
    Task<List<IpRequestHistoryItemDto>> GetAllRequestsAsync(int userId, CancellationToken ct = default);

    Task<IpChangeRequestStatusDto> RaiseIpChangeRequestAsync(
        int userId, RaiseIpChangeRequestDto request, CancellationToken ct = default);
}
