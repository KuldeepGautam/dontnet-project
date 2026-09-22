namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the ECL microservice. Added 2026-08-18 (Part 2 — UBIS_Web front-end).
/// Mirrors PreBudgetClient's shape (SendAsync helper, empty-body-safe deserialization, resilience-aware
/// failure handling) — see that class's comments for the rationale behind each choice repeated here.</summary>
public class EclClient : IEclClient
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public EclClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public Task<ApiCallResult<List<EclSchemeOutlayDto>>> GetOutlaysAsync(string bearerToken, int? demandId, int? categoryId, int? schemeId, string financialYear, CancellationToken ct = default)
    {
        var query = $"api/ecl/outlay?financialYear={Uri.EscapeDataString(financialYear ?? string.Empty)}";
        if (demandId.HasValue) query += $"&demandId={demandId.Value}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId.Value}";
        if (schemeId.HasValue) query += $"&schemeId={schemeId.Value}";
        return SendAsync<List<EclSchemeOutlayDto>>(HttpMethod.Get, query, body: null, bearerToken, ct);
    }

    public Task<ApiCallResult<EclSchemeOutlayDto>> GetOutlayAsync(string bearerToken, int rowId, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Get, $"api/ecl/outlay/{rowId}", body: null, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> CreateOutlayAsync(string bearerToken, SaveSchemeOutlayRequestDto request, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, "api/ecl/outlay", request, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> UpdateOutlayAsync(string bearerToken, int rowId, SaveSchemeOutlayRequestDto request, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Put, $"api/ecl/outlay/{rowId}", request, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> SubmitForApprovalAsync(string bearerToken, int rowId, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, $"api/ecl/outlay/{rowId}/submit-for-approval", body: null, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> RecordActualsAsync(string bearerToken, int rowId, decimal?[] actuals, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, $"api/ecl/outlay/{rowId}/actuals", new EclActualsRequestDto { Actuals = actuals }, bearerToken, ct);

    public async Task<ApiCallResult<EclDocumentUploadResultDto>> UploadDocumentAsync(string bearerToken, Stream fileStream, string fileName, string? contentType, int demandId, int schemeId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ecl/outlay/documents");
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(demandId.ToString()), "demandId");
        content.Add(new StringContent(schemeId.ToString()), "schemeId");
        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _logClient.ErrorAsync("ECL document upload failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { fileName }, ct).ConfigureAwait(false);
            return ApiCallResult<EclDocumentUploadResultDto>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The ECL service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var data = string.IsNullOrWhiteSpace(raw) ? default : System.Text.Json.JsonSerializer.Deserialize<EclDocumentUploadResultDto>(raw, JsonOptions);
                return ApiCallResult<EclDocumentUploadResultDto>.Success(data!, statusCode);
            }

            AimErrorDto? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<AimErrorDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch
            {
                // Some failure responses carry no JSON body.
            }

            return ApiCallResult<EclDocumentUploadResultDto>.Failure(statusCode, error);
        }
    }

    public async Task<ApiCallResult<byte[]>> GetDocumentAsync(string bearerToken, int rowId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/ecl/outlay/{rowId}/document");
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
            await _logClient.ErrorAsync("ECL document fetch failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { rowId }, ct).ConfigureAwait(false);
            return ApiCallResult<byte[]>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The ECL service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                return ApiCallResult<byte[]>.Success(bytes, statusCode);
            }

            AimErrorDto? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<AimErrorDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch
            {
                // Some failure responses carry no JSON body.
            }

            return ApiCallResult<byte[]>.Failure(statusCode, error);
        }
    }

    public async Task<ApiCallResult<bool>> DeleteOutlayAsync(string bearerToken, int rowId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/ecl/outlay/{rowId}");
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
            await _logClient.ErrorAsync("ECL delete failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { rowId }, ct).ConfigureAwait(false);
            return ApiCallResult<bool>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The ECL service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                return ApiCallResult<bool>.Success(true, statusCode);
            }

            AimErrorDto? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<AimErrorDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch
            {
                // Some failure responses carry no JSON body (e.g. 204/404 without one).
            }

            return ApiCallResult<bool>.Failure(statusCode, error);
        }
    }

    public Task<ApiCallResult<List<EclCategoryDto>>> GetCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<EclCategoryDto>>(HttpMethod.Get, $"api/ecl/outlay/categories?financialYear={Uri.EscapeDataString(financialYear ?? string.Empty)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<EclSchemeDto>>> GetSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default) =>
        SendAsync<List<EclSchemeDto>>(HttpMethod.Get, $"api/ecl/outlay/schemes?demandId={demandId}&categoryId={categoryId}", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<EclUmbSchemeDto>>> GetUmbrellaSchemesAsync(string bearerToken, int categoryId, int demandId, CancellationToken ct = default) =>
        SendAsync<List<EclUmbSchemeDto>>(HttpMethod.Get, $"api/ecl/outlay/umbrella-schemes?categoryId={categoryId}&demandId={demandId}", body: null, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeDto>> CreateSchemeAsync(string bearerToken, CreateSchemeRequestDto request, CancellationToken ct = default) =>
        SendAsync<EclSchemeDto>(HttpMethod.Post, "api/ecl/outlay/schemes", request, bearerToken, ct);

    public Task<ApiCallResult<List<EclApprovalAuthorityDto>>> GetApprovalAuthoritiesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<EclApprovalAuthorityDto>>(HttpMethod.Get, "api/ecl/outlay/authorities", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<EclAppraiseAuthorityDto>>> GetAppraiseAuthoritiesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<EclAppraiseAuthorityDto>>(HttpMethod.Get, "api/ecl/outlay/appraise-authorities", body: null, bearerToken, ct);

    public Task<ApiCallResult<EclFinancialYearOptionsDto>> GetFinancialYearsAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<EclFinancialYearOptionsDto>(HttpMethod.Get, "api/ecl/outlay/financial-years", body: null, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> ApproveAsync(string bearerToken, int rowId, string? doeRemarks, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, $"api/ecl/approval/{rowId}/approve", new DoeApprovalActionRequestDto { DoeRemarks = doeRemarks }, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> RejectAsync(string bearerToken, int rowId, string? doeRemarks, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, $"api/ecl/approval/{rowId}/reject", new DoeApprovalActionRequestDto { DoeRemarks = doeRemarks }, bearerToken, ct);

    public Task<ApiCallResult<EclSchemeOutlayDto>> RequestReapprovalAsync(string bearerToken, int rowId, CancellationToken ct = default) =>
        SendAsync<EclSchemeOutlayDto>(HttpMethod.Post, $"api/ecl/approval/{rowId}/request-reapproval", body: null, bearerToken, ct);

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
            await _logClient.ErrorAsync(
                "ECL call failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { relativeUrl }, ct).ConfigureAwait(false);
            return ApiCallResult<TResponse>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The ECL service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var data = string.IsNullOrWhiteSpace(raw)
                    ? default
                    : System.Text.Json.JsonSerializer.Deserialize<TResponse>(raw, JsonOptions);
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
