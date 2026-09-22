namespace UBIS.Web.Services.Clients;

public interface IUserProfileClient
{
    Task<ApiCallResult<UserProfileSummaryDto>> GetMyProfileAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<List<IpRequestHistoryItemDto>>> GetIpRequestHistoryAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<IpChangeRequestStatusDto>> RaiseIpChangeRequestAsync(
        string bearerToken, RaiseIpChangeRequestDto request, CancellationToken ct = default);

    Task<ApiCallResult<object?>> UpdateContactAsync(
        string bearerToken, UpdateContactRequestDto request, CancellationToken ct = default);
}
