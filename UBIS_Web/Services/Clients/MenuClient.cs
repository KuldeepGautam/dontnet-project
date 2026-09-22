namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the MenuGenerator microservice.</summary>
public class MenuClient : IMenuClient
{
    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public MenuClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public async Task<ApiCallResult<MenuFullResponseDto>> GetFullMenuAsync(string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/menu/full");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Retries (5) and the circuit breaker are already exhausted by the time we get
            // here (or the caller's own request was cancelled, e.g. a browser navigate-away).
            // Log centrally and hand the caller an empty-but-valid menu rather than an unhandled
            // exception — a user should still reach their Dashboard even if the sidebar is
            // temporarily unavailable. Was previously only catching this for the resilience-
            // timeout case and letting genuine caller cancellation propagate unhandled - that
            // distinction wasn't buying anything (tester-reported 2026-08-03 for the equivalent
            // AimClient gap) since the response goes nowhere useful either way once cancelled.
            await _logClient.ErrorAsync(
                "MenuGenerator call failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, ct: ct).ConfigureAwait(false);
            return ApiCallResult<MenuFullResponseDto>.Failure(
                StatusCodes.Status503ServiceUnavailable, null);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<MenuFullResponseDto>(cancellationToken: ct).ConfigureAwait(false);
                return ApiCallResult<MenuFullResponseDto>.Success(data ?? new MenuFullResponseDto(new()), statusCode);
            }

            return ApiCallResult<MenuFullResponseDto>.Failure(statusCode, null);
        }
    }
}
