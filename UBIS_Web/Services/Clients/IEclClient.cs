namespace UBIS.Web.Services.Clients;

/// <summary>Typed HttpClient for the ECL microservice (Domain\ECL). Added 2026-08-18 (Part 2).</summary>
public interface IEclClient
{
    // --- Outlay CRUD / workflow (EclOutlayController) ---
    Task<ApiCallResult<List<EclSchemeOutlayDto>>> GetOutlaysAsync(string bearerToken, int? demandId, int? categoryId, int? schemeId, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> GetOutlayAsync(string bearerToken, int rowId, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> CreateOutlayAsync(string bearerToken, SaveSchemeOutlayRequestDto request, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> UpdateOutlayAsync(string bearerToken, int rowId, SaveSchemeOutlayRequestDto request, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> SubmitForApprovalAsync(string bearerToken, int rowId, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> RecordActualsAsync(string bearerToken, int rowId, decimal?[] actuals, CancellationToken ct = default);

    Task<ApiCallResult<EclDocumentUploadResultDto>> UploadDocumentAsync(string bearerToken, Stream fileStream, string fileName, string? contentType, int demandId, int schemeId, CancellationToken ct = default);

    /// <summary>Buffers the whole file into memory (capped at 5 MB by the ECL service itself, so this is bounded) rather than proxying a live stream — avoids HttpResponseMessage lifetime/disposal complexity for a same-box hop this small.</summary>
    Task<ApiCallResult<byte[]>> GetDocumentAsync(string bearerToken, int rowId, CancellationToken ct = default);

    Task<ApiCallResult<bool>> DeleteOutlayAsync(string bearerToken, int rowId, CancellationToken ct = default);

    // --- Lookups ---
    Task<ApiCallResult<List<EclCategoryDto>>> GetCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<List<EclSchemeDto>>> GetSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default);

    Task<ApiCallResult<List<EclUmbSchemeDto>>> GetUmbrellaSchemesAsync(string bearerToken, int categoryId, int demandId, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeDto>> CreateSchemeAsync(string bearerToken, CreateSchemeRequestDto request, CancellationToken ct = default);

    Task<ApiCallResult<List<EclApprovalAuthorityDto>>> GetApprovalAuthoritiesAsync(string bearerToken, CancellationToken ct = default);

    /// <summary>Independent from GetApprovalAuthoritiesAsync — see EclAppraiseAuthorityDto's doc comment.</summary>
    Task<ApiCallResult<List<EclAppraiseAuthorityDto>>> GetAppraiseAuthoritiesAsync(string bearerToken, CancellationToken ct = default);

    Task<ApiCallResult<EclFinancialYearOptionsDto>> GetFinancialYearsAsync(string bearerToken, CancellationToken ct = default);

    // --- DOE approval (EclApprovalController, DOE-only) ---
    Task<ApiCallResult<EclSchemeOutlayDto>> ApproveAsync(string bearerToken, int rowId, string? doeRemarks, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> RejectAsync(string bearerToken, int rowId, string? doeRemarks, CancellationToken ct = default);

    Task<ApiCallResult<EclSchemeOutlayDto>> RequestReapprovalAsync(string bearerToken, int rowId, CancellationToken ct = default);
}
