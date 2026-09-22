namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the UserProfile microservice.</summary>
public class UserProfileClient : IUserProfileClient
{
    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public UserProfileClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public Task<ApiCallResult<UserProfileSummaryDto>> GetMyProfileAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<UserProfileSummaryDto>(HttpMethod.Get, "api/userprofile/me", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<IpRequestHistoryItemDto>>> GetIpRequestHistoryAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<IpRequestHistoryItemDto>>(HttpMethod.Get, "api/userprofile/ip-requests", body: null, bearerToken, ct);

    public Task<ApiCallResult<IpChangeRequestStatusDto>> RaiseIpChangeRequestAsync(
        string bearerToken, RaiseIpChangeRequestDto request, CancellationToken ct = default) =>
        SendAsync<IpChangeRequestStatusDto>(HttpMethod.Post, "api/userprofile/ip-change-request", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> UpdateContactAsync(
        string bearerToken, UpdateContactRequestDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Put, "api/userprofile/contact", request, bearerToken, ct);

    private async Task<ApiCallResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method, string relativeUrl, object? body, string? bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, relativeUrl);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Was "when (ex is not OperationCanceledException)" - excluded ALL cancellation,
            // including the resilience pipeline's own timeout, not just genuine caller
            // cancellation (tester-reported 2026-08-03 for the equivalent AimClient gap). Retries
            // (5) and the circuit breaker (Program.cs's AddStandardResilienceHandler config) are
            // already exhausted by the time we get here either way — this is the final give-up
            // failure. Log it centrally and hand the caller a graceful result instead of an
            // unhandled exception.
            await _logClient.ErrorAsync(
                "UserProfile call failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { relativeUrl }, ct).ConfigureAwait(false);
            return ApiCallResult<TResponse>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The profile service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct).ConfigureAwait(false);
                return ApiCallResult<TResponse>.Success(data!, statusCode);
            }

            AimErrorDto? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<AimErrorDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch
            {
                // Some failure responses carry no JSON body — statusCode alone drives the message.
            }

            return ApiCallResult<TResponse>.Failure(statusCode, error);
        }
    }
}
