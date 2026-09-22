namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the Reporting microservice. Mirrors EclClient.GetDocumentAsync's shape (buffer the whole file into memory — bounded, same-box hop) but as a POST with a JSON body instead of a GET.</summary>
public class ReportingClient : IReportingClient
{
    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public ReportingClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public Task<ApiCallResult<byte[]>> ExportPdfAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default) =>
        ExportAsync("api/reports/pdf", request, bearerToken, ct);

    public Task<ApiCallResult<byte[]>> ExportExcelAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default) =>
        ExportAsync("api/reports/excel", request, bearerToken, ct);

    public Task<ApiCallResult<byte[]>> ExportCsvAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default) =>
        ExportAsync("api/reports/csv", request, bearerToken, ct);

    private async Task<ApiCallResult<byte[]>> ExportAsync(string path, TabularReportDto request, string bearerToken, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request)
        };
        if (!string.IsNullOrEmpty(bearerToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(httpRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _logClient.ErrorAsync("Reporting export failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { path, request.Title }, ct).ConfigureAwait(false);
            return ApiCallResult<byte[]>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The Reporting service is temporarily unavailable. Please try again shortly." });
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
}
