namespace UBIS.Web.Services.Clients;

/// <summary>Typed HttpClient for the generic Reporting microservice (Core/Reporting). Added 2026-08-20, CSV added 2026-09-07. Any page that needs an "Export to PDF"/"Export to Excel"/"Export to CSV" button builds its own TabularReportDto and calls one of these three methods — no per-page endpoint needed.</summary>
public interface IReportingClient
{
    Task<ApiCallResult<byte[]>> ExportPdfAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default);

    Task<ApiCallResult<byte[]>> ExportExcelAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default);

    Task<ApiCallResult<byte[]>> ExportCsvAsync(string bearerToken, TabularReportDto request, CancellationToken ct = default);
}
