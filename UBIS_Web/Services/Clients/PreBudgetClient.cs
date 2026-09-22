namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the PreBudget microservice. Added 2026-07-23.</summary>
public class PreBudgetClient : IPreBudgetClient
{
    // Matches the defaults ReadFromJsonAsync<T>() uses internally (camelCase, case-insensitive) -
    // needed because the empty-body guard below deserializes manually instead of via that helper.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public PreBudgetClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public Task<ApiCallResult<List<PreBudgetAppendixDto>>> GetAppendicesAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<PreBudgetAppendixDto>>(HttpMethod.Get, $"api/appendices?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<PreBudgetAppendixWithStatusDto>>> GetAppendicesWithStatusAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<PreBudgetAppendixWithStatusDto>>(HttpMethod.Get, $"api/appendices/status?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AllocationResultDto>> AllocateAsync(string bearerToken, AllocateAppendixDto request, CancellationToken ct = default) =>
        SendAsync<AllocationResultDto>(HttpMethod.Post, "api/appendices/allocate", request, bearerToken, ct);

    public Task<ApiCallResult<List<AllocationListItemDto>>> GetAllocationsAsync(string bearerToken, int? appendixId, string financialYear, CancellationToken ct = default)
    {
        var query = $"api/appendices/allocations?financialYear={Uri.EscapeDataString(financialYear)}";
        if (appendixId.HasValue)
        {
            query += $"&appendixId={appendixId.Value}";
        }
        return SendAsync<List<AllocationListItemDto>>(HttpMethod.Get, query, body: null, bearerToken, ct);
    }

    public Task<ApiCallResult<object?>> UpdateAllocationAsync(string bearerToken, UpdateAllocationDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Put, "api/appendices/allocations", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> SetNilSubmissionAsync(string bearerToken, SetNilSubmissionDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/appendices/nil-submission", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> FreezeSubmissionAsync(string bearerToken, int appendixId, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, $"api/appendices/{appendixId}/freeze?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<int>>> GetAllocatedDemandIdsAsync(string bearerToken, IEnumerable<int> demandIds, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<int>>(HttpMethod.Get, $"api/appendices/allocated-demand-ids?demandIds={Uri.EscapeDataString(string.Join(",", demandIds))}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    // --- Appendix I ---
    public Task<ApiCallResult<List<AppendixBudgetExpenditureTrendDto>>> GetAppendixIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixBudgetExpenditureTrendDto>>(HttpMethod.Get, $"api/appendix-i?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixBudgetExpenditureTrendDto?>> GetAppendixIByYearAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<AppendixBudgetExpenditureTrendDto?>(HttpMethod.Get, $"api/appendix-i/by-year?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> CreateAppendixIAsync(string bearerToken, SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct = default) =>
        SendAsync<AppendixBudgetExpenditureTrendDto>(HttpMethod.Post, "api/appendix-i", request, bearerToken, ct);

    public Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> UpdateAppendixIAsync(string bearerToken, int id, SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct = default) =>
        SendAsync<AppendixBudgetExpenditureTrendDto>(HttpMethod.Put, $"api/appendix-i/{id}", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> DeleteAppendixIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-i/{id}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> FreezeAppendixIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixBudgetExpenditureTrendDto>(HttpMethod.Post, $"api/appendix-i/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix II ---
    public Task<ApiCallResult<List<AppendixQuarterlyExpenditurePlanDto>>> GetAppendixIIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixQuarterlyExpenditurePlanDto>>(HttpMethod.Get, $"api/appendix-ii?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> CreateAppendixIIAsync(string bearerToken, SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct = default) =>
        SendAsync<AppendixQuarterlyExpenditurePlanDto>(HttpMethod.Post, "api/appendix-ii", request, bearerToken, ct);

    public Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> UpdateAppendixIIAsync(string bearerToken, int id, SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct = default) =>
        SendAsync<AppendixQuarterlyExpenditurePlanDto>(HttpMethod.Put, $"api/appendix-ii/{id}", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> DeleteAppendixIIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-ii/{id}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> FreezeAppendixIIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixQuarterlyExpenditurePlanDto>(HttpMethod.Post, $"api/appendix-ii/{id}/freeze", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixIIPreviousYearApprovedQepDto>> GetAppendixIIPreviousYearApprovedQepAsync(string bearerToken, int demandId, CancellationToken ct = default) =>
        SendAsync<AppendixIIPreviousYearApprovedQepDto>(HttpMethod.Get, $"api/appendix-ii/previous-year-approved-qep?demandId={demandId}", body: null, bearerToken, ct);

    // --- Appendix III-A ---
    public Task<ApiCallResult<List<AppendixTsaAssignmentDto>>> GetAppendixIIIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixTsaAssignmentDto>>(HttpMethod.Get, $"api/appendix-iiia?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixTsaAssignmentDto>> CreateAppendixIIIAAsync(string bearerToken, SaveAppendixTsaAssignmentDto request, CancellationToken ct = default) =>
        SendAsync<AppendixTsaAssignmentDto>(HttpMethod.Post, "api/appendix-iiia", request, bearerToken, ct);

    public Task<ApiCallResult<AppendixTsaAssignmentDto>> UpdateAppendixIIIAAsync(string bearerToken, int id, SaveAppendixTsaAssignmentDto request, CancellationToken ct = default) =>
        SendAsync<AppendixTsaAssignmentDto>(HttpMethod.Put, $"api/appendix-iiia/{id}", request, bearerToken, ct);

    public Task<ApiCallResult<object?>> DeleteAppendixIIIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-iiia/{id}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixTsaAssignmentDto>> FreezeAppendixIIIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixTsaAssignmentDto>(HttpMethod.Post, $"api/appendix-iiia/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix I-A ---
    public Task<ApiCallResult<List<AppendixProjectedDemandDto>>> GetAppendixIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixProjectedDemandDto>>(HttpMethod.Get, $"api/appendix-ia?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixProjectedDemandDto>> CreateAppendixIAAsync(string bearerToken, SaveAppendixProjectedDemandDto request, CancellationToken ct = default) =>
        SendAsync<AppendixProjectedDemandDto>(HttpMethod.Post, "api/appendix-ia", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixProjectedDemandDto>> UpdateAppendixIAAsync(string bearerToken, int id, SaveAppendixProjectedDemandDto request, CancellationToken ct = default) =>
        SendAsync<AppendixProjectedDemandDto>(HttpMethod.Put, $"api/appendix-ia/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-ia/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixProjectedDemandDto>> FreezeAppendixIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixProjectedDemandDto>(HttpMethod.Post, $"api/appendix-ia/{id}/freeze", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixIAPreviousYearBeDto>> GetAppendixIAPreviousYearBeAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<AppendixIAPreviousYearBeDto>(HttpMethod.Get, $"api/appendix-ia/previous-year-be?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    // --- Appendix III ---
    public Task<ApiCallResult<List<AppendixCnaSnaBalanceDto>>> GetAppendixIIIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixCnaSnaBalanceDto>>(HttpMethod.Get, $"api/appendix-iii?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCnaSnaBalanceDto>> CreateAppendixIIIAsync(string bearerToken, SaveAppendixCnaSnaBalanceDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCnaSnaBalanceDto>(HttpMethod.Post, "api/appendix-iii", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixCnaSnaBalanceDto>> UpdateAppendixIIIAsync(string bearerToken, int id, SaveAppendixCnaSnaBalanceDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCnaSnaBalanceDto>(HttpMethod.Put, $"api/appendix-iii/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIIIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-iii/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCnaSnaBalanceDto>> FreezeAppendixIIIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixCnaSnaBalanceDto>(HttpMethod.Post, $"api/appendix-iii/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIICategoryOptionDto>>> GetAppendixIIICategoriesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIICategoryOptionDto>>(HttpMethod.Get, "api/appendix-iii/categories", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIIISchemesAsync(string bearerToken, int demandId, string categoryType, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISchemeOptionDto>>(HttpMethod.Get, $"api/appendix-iii/schemes?demandId={demandId}&categoryType={Uri.EscapeDataString(categoryType)}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixIIISubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISubSchemeOptionDto>>(HttpMethod.Get, $"api/appendix-iii/subschemes?schemeId={schemeId}", body: null, bearerToken, ct);

    // --- Appendix IV ---
    public Task<ApiCallResult<List<AppendixEstimatesOfSchemesDto>>> GetAppendixIVAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixEstimatesOfSchemesDto>>(HttpMethod.Get, $"api/appendix-iv?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> CreateAppendixIVAsync(string bearerToken, SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstimatesOfSchemesDto>(HttpMethod.Post, "api/appendix-iv", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> UpdateAppendixIVAsync(string bearerToken, int id, SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstimatesOfSchemesDto>(HttpMethod.Put, $"api/appendix-iv/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIVAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-iv/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> FreezeAppendixIVAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixEstimatesOfSchemesDto>(HttpMethod.Post, $"api/appendix-iv/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIVSchemesAsync(string bearerToken, int demandId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISchemeOptionDto>>(HttpMethod.Get, $"api/appendix-iv/schemes?demandId={demandId}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixIVSubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISubSchemeOptionDto>>(HttpMethod.Get, $"api/appendix-iv/subschemes?schemeId={schemeId}", body: null, bearerToken, ct);

    // --- Appendix IV-A ---
    public Task<ApiCallResult<List<AppendixScspExpenditureDto>>> GetAppendixIVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixScspExpenditureDto>>(HttpMethod.Get, $"api/appendix-iva?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixScspExpenditureDto>> CreateAppendixIVAAsync(string bearerToken, SaveAppendixScspExpenditureDto request, CancellationToken ct = default) =>
        SendAsync<AppendixScspExpenditureDto>(HttpMethod.Post, "api/appendix-iva", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixScspExpenditureDto>> UpdateAppendixIVAAsync(string bearerToken, int id, SaveAppendixScspExpenditureDto request, CancellationToken ct = default) =>
        SendAsync<AppendixScspExpenditureDto>(HttpMethod.Put, $"api/appendix-iva/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIVAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-iva/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixScspExpenditureDto>> FreezeAppendixIVAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixScspExpenditureDto>(HttpMethod.Post, $"api/appendix-iva/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix IV-B ---
    public Task<ApiCallResult<List<AppendixTaspExpenditureDto>>> GetAppendixIVBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixTaspExpenditureDto>>(HttpMethod.Get, $"api/appendix-ivb?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixTaspExpenditureDto>> CreateAppendixIVBAsync(string bearerToken, SaveAppendixTaspExpenditureDto request, CancellationToken ct = default) =>
        SendAsync<AppendixTaspExpenditureDto>(HttpMethod.Post, "api/appendix-ivb", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixTaspExpenditureDto>> UpdateAppendixIVBAsync(string bearerToken, int id, SaveAppendixTaspExpenditureDto request, CancellationToken ct = default) =>
        SendAsync<AppendixTaspExpenditureDto>(HttpMethod.Put, $"api/appendix-ivb/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIVBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-ivb/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixTaspExpenditureDto>> FreezeAppendixIVBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixTaspExpenditureDto>(HttpMethod.Post, $"api/appendix-ivb/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix V ---
    public Task<ApiCallResult<List<AppendixEstablishmentExpenditureDto>>> GetAppendixVAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixEstablishmentExpenditureDto>>(HttpMethod.Get, $"api/appendix-v?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentExpenditureDto>> CreateAppendixVAsync(string bearerToken, SaveAppendixEstablishmentExpenditureDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentExpenditureDto>(HttpMethod.Post, "api/appendix-v", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-v/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentExpenditureDto>> FreezeAppendixVAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentExpenditureDto>(HttpMethod.Post, $"api/appendix-v/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix V-A ---
    public Task<ApiCallResult<List<AppendixGrantInAidDto>>> GetAppendixVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixGrantInAidDto>>(HttpMethod.Get, $"api/appendix-va?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixGrantInAidDto>> CreateAppendixVAAsync(string bearerToken, SaveAppendixGrantInAidDto request, CancellationToken ct = default) =>
        SendAsync<AppendixGrantInAidDto>(HttpMethod.Post, "api/appendix-va", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixGrantInAidDto>> UpdateAppendixVAAsync(string bearerToken, int id, SaveAppendixGrantInAidDto request, CancellationToken ct = default) =>
        SendAsync<AppendixGrantInAidDto>(HttpMethod.Put, $"api/appendix-va/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-va/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixGrantInAidDto>> FreezeAppendixVAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixGrantInAidDto>(HttpMethod.Post, $"api/appendix-va/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix V-B ---
    public Task<ApiCallResult<List<AppendixEstablishmentByObjectHeadDto>>> GetAppendixVBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixEstablishmentByObjectHeadDto>>(HttpMethod.Get, $"api/appendix-vb?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> CreateAppendixVBAsync(string bearerToken, SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentByObjectHeadDto>(HttpMethod.Post, "api/appendix-vb", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> UpdateAppendixVBAsync(string bearerToken, int id, SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentByObjectHeadDto>(HttpMethod.Put, $"api/appendix-vb/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vb/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> FreezeAppendixVBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentByObjectHeadDto>(HttpMethod.Post, $"api/appendix-vb/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVBObjectHeadOptionDto>>> GetAppendixVBObjectHeadsAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixVBObjectHeadOptionDto>>(HttpMethod.Get, $"api/appendix-vb/object-heads?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    // --- Appendix V-C ---
    public Task<ApiCallResult<List<AppendixEstablishmentOtherThanABDto>>> GetAppendixVCAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixEstablishmentOtherThanABDto>>(HttpMethod.Get, $"api/appendix-vc?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> CreateAppendixVCAsync(string bearerToken, SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentOtherThanABDto>(HttpMethod.Post, "api/appendix-vc", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> UpdateAppendixVCAsync(string bearerToken, int id, SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentOtherThanABDto>(HttpMethod.Put, $"api/appendix-vc/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVCAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vc/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> FreezeAppendixVCAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixEstablishmentOtherThanABDto>(HttpMethod.Post, $"api/appendix-vc/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VI ---
    public Task<ApiCallResult<List<AppendixNonTaxRevenueDto>>> GetAppendixVIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixNonTaxRevenueDto>>(HttpMethod.Get, $"api/appendix-vi?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixNonTaxRevenueDto>> CreateAppendixVIAsync(string bearerToken, SaveAppendixNonTaxRevenueDto request, CancellationToken ct = default) =>
        SendAsync<AppendixNonTaxRevenueDto>(HttpMethod.Post, "api/appendix-vi", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixNonTaxRevenueDto>> UpdateAppendixVIAsync(string bearerToken, int id, SaveAppendixNonTaxRevenueDto request, CancellationToken ct = default) =>
        SendAsync<AppendixNonTaxRevenueDto>(HttpMethod.Put, $"api/appendix-vi/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vi/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixNonTaxRevenueDto>> FreezeAppendixVIAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixNonTaxRevenueDto>(HttpMethod.Post, $"api/appendix-vi/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIReceiptTypeOptionDto>>> GetAppendixVIReceiptTypesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIReceiptTypeOptionDto>>(HttpMethod.Get, "api/appendix-vi/receipt-types", body: null, bearerToken, ct);

    // --- Appendix VI-A ---
    public Task<ApiCallResult<List<AppendixUserChargesDto>>> GetAppendixVIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixUserChargesDto>>(HttpMethod.Get, $"api/appendix-via?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesDto>> CreateAppendixVIAAsync(string bearerToken, SaveAppendixUserChargesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesDto>(HttpMethod.Post, "api/appendix-via", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesDto>> UpdateAppendixVIAAsync(string bearerToken, int id, SaveAppendixUserChargesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesDto>(HttpMethod.Put, $"api/appendix-via/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-via/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesDto>> FreezeAppendixVIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesDto>(HttpMethod.Post, $"api/appendix-via/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VI-B ---
    public Task<ApiCallResult<List<AppendixPendingLiabilitiesDto>>> GetAppendixVIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixPendingLiabilitiesDto>>(HttpMethod.Get, $"api/appendix-vib?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixPendingLiabilitiesDto>> CreateAppendixVIBAsync(string bearerToken, SaveAppendixPendingLiabilitiesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixPendingLiabilitiesDto>(HttpMethod.Post, "api/appendix-vib", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixPendingLiabilitiesDto>> UpdateAppendixVIBAsync(string bearerToken, int id, SaveAppendixPendingLiabilitiesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixPendingLiabilitiesDto>(HttpMethod.Put, $"api/appendix-vib/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vib/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixPendingLiabilitiesDto>> FreezeAppendixVIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixPendingLiabilitiesDto>(HttpMethod.Post, $"api/appendix-vib/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIBCategoryOptionDto>>> GetAppendixVIBCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIBCategoryOptionDto>>(HttpMethod.Get, $"api/appendix-vib/categories?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixIIIBeByStructureAsync(string bearerToken, int demandId, int schemeId, CancellationToken ct = default) =>
        SendAsync<AppendixBeAutoLoadDto>(HttpMethod.Get, $"api/appendix-iii/be-by-scheme?demandId={demandId}&schemeId={schemeId}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixIVBeByStructureAsync(string bearerToken, int demandId, int schemeId, int? subSchemeId, string financialYear, CancellationToken ct = default) =>
        SendAsync<AppendixBeAutoLoadDto>(HttpMethod.Get, $"api/appendix-iv/be-by-scheme?demandId={demandId}&schemeId={schemeId}&subSchemeId={subSchemeId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppendixVBBeActualsDto>> GetAppendixVBPreviousYearBeAsync(string bearerToken, int demandId, int objectHeadId, CancellationToken ct = default) =>
        SendAsync<AppendixVBBeActualsDto>(HttpMethod.Get, $"api/appendix-vb/previous-year-be?demandId={demandId}&objectHeadId={objectHeadId}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixVIBSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISchemeOptionDto>>(HttpMethod.Get, $"api/appendix-vib/schemes?demandId={demandId}&categoryId={categoryId}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixVIBSubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISubSchemeOptionDto>>(HttpMethod.Get, $"api/appendix-vib/subschemes?schemeId={schemeId}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixVIBBeAsync(string bearerToken, int demandId, int categoryId, int schemeId, CancellationToken ct = default) =>
        SendAsync<AppendixBeAutoLoadDto>(HttpMethod.Get, $"api/appendix-vib/be?demandId={demandId}&categoryId={categoryId}&schemeId={schemeId}", body: null, bearerToken, ct);

    // --- Appendix VI-C ---
    public Task<ApiCallResult<List<AppendixCorpusFundDto>>> GetAppendixVICAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixCorpusFundDto>>(HttpMethod.Get, $"api/appendix-vic?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundDto>> CreateAppendixVICAsync(string bearerToken, SaveAppendixCorpusFundDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundDto>(HttpMethod.Post, "api/appendix-vic", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundDto>> UpdateAppendixVICAsync(string bearerToken, int id, SaveAppendixCorpusFundDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundDto>(HttpMethod.Put, $"api/appendix-vic/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVICAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vic/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundDto>> FreezeAppendixVICAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundDto>(HttpMethod.Post, $"api/appendix-vic/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VI-D ---
    public Task<ApiCallResult<List<AppendixInternalResourcesDto>>> GetAppendixVIDAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixInternalResourcesDto>>(HttpMethod.Get, $"api/appendix-vid?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixInternalResourcesDto>> CreateAppendixVIDAsync(string bearerToken, SaveAppendixInternalResourcesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixInternalResourcesDto>(HttpMethod.Post, "api/appendix-vid", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixInternalResourcesDto>> UpdateAppendixVIDAsync(string bearerToken, int id, SaveAppendixInternalResourcesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixInternalResourcesDto>(HttpMethod.Put, $"api/appendix-vid/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIDAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vid/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixInternalResourcesDto>> FreezeAppendixVIDAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixInternalResourcesDto>(HttpMethod.Post, $"api/appendix-vid/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VI-E ---
    public Task<ApiCallResult<List<AppendixCorpusFundAbGiaDto>>> GetAppendixVIEAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixCorpusFundAbGiaDto>>(HttpMethod.Get, $"api/appendix-vie?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> CreateAppendixVIEAsync(string bearerToken, SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundAbGiaDto>(HttpMethod.Post, "api/appendix-vie", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> UpdateAppendixVIEAsync(string bearerToken, int id, SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundAbGiaDto>(HttpMethod.Put, $"api/appendix-vie/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIEAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vie/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> FreezeAppendixVIEAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixCorpusFundAbGiaDto>(HttpMethod.Post, $"api/appendix-vie/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VI-F ---
    public Task<ApiCallResult<List<AppendixMinorHeadUserChargesDto>>> GetAppendixVIFAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixMinorHeadUserChargesDto>>(HttpMethod.Get, $"api/appendix-vif?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> CreateAppendixVIFAsync(string bearerToken, SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixMinorHeadUserChargesDto>(HttpMethod.Post, "api/appendix-vif", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> UpdateAppendixVIFAsync(string bearerToken, int id, SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixMinorHeadUserChargesDto>(HttpMethod.Put, $"api/appendix-vif/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIFAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vif/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> FreezeAppendixVIFAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixMinorHeadUserChargesDto>(HttpMethod.Post, $"api/appendix-vif/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<MinorHeadSuggestionDto>>> SearchMinorHeadsAsync(string bearerToken, int demandId, string financialYear, string query, CancellationToken ct = default) =>
        SendAsync<List<MinorHeadSuggestionDto>>(HttpMethod.Get, $"api/appendix-vif/minor-heads?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}&query={Uri.EscapeDataString(query)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<MinorHeadNameDto>> GetMinorHeadNameAsync(string bearerToken, string minorHeadCode, string financialYear, CancellationToken ct = default) =>
        SendAsync<MinorHeadNameDto>(HttpMethod.Get, $"api/appendix-vif/minor-head-name?minorHeadCode={Uri.EscapeDataString(minorHeadCode)}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    // --- Appendix III-B ---
    public Task<ApiCallResult<List<AppendixSchemeAppraisalStatusDto>>> GetAppendixIIIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixSchemeAppraisalStatusDto>>(HttpMethod.Get, $"api/appendix-iiib?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> CreateAppendixIIIBAsync(string bearerToken, SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct = default) =>
        SendAsync<AppendixSchemeAppraisalStatusDto>(HttpMethod.Post, "api/appendix-iiib", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> UpdateAppendixIIIBAsync(string bearerToken, int id, SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct = default) =>
        SendAsync<AppendixSchemeAppraisalStatusDto>(HttpMethod.Put, $"api/appendix-iiib/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixIIIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-iiib/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> FreezeAppendixIIIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixSchemeAppraisalStatusDto>(HttpMethod.Post, $"api/appendix-iiib/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIIBCategoryOptionDto>>> GetAppendixIIIBCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIIBCategoryOptionDto>>(HttpMethod.Get, $"api/appendix-iiib/categories?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIIIBSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default) =>
        SendAsync<List<AppendixIIISchemeOptionDto>>(HttpMethod.Get, $"api/appendix-iiib/schemes?demandId={demandId}&categoryId={categoryId}", body: null, bearerToken, ct);

    // --- Appendix VI-G ---
    public Task<ApiCallResult<List<AppendixUserChargesAutonomousBodyDto>>> GetAppendixVIGAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixUserChargesAutonomousBodyDto>>(HttpMethod.Get, $"api/appendix-vig?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> CreateAppendixVIGAsync(string bearerToken, SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesAutonomousBodyDto>(HttpMethod.Post, "api/appendix-vig", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> UpdateAppendixVIGAsync(string bearerToken, int id, SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesAutonomousBodyDto>(HttpMethod.Put, $"api/appendix-vig/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIGAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-vig/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> FreezeAppendixVIGAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixUserChargesAutonomousBodyDto>(HttpMethod.Post, $"api/appendix-vig/{id}/freeze", body: null, bearerToken, ct);

    // --- Appendix VII-A ---
    public Task<ApiCallResult<List<AppendixRecoveriesDto>>> GetAppendixVIIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixRecoveriesDto>>(HttpMethod.Get, $"api/appendix-viia?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixRecoveriesDto>> CreateAppendixVIIAAsync(string bearerToken, SaveAppendixRecoveriesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixRecoveriesDto>(HttpMethod.Post, "api/appendix-viia", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixRecoveriesDto>> UpdateAppendixVIIAAsync(string bearerToken, int id, SaveAppendixRecoveriesDto request, CancellationToken ct = default) =>
        SendAsync<AppendixRecoveriesDto>(HttpMethod.Put, $"api/appendix-viia/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-viia/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixRecoveriesDto>> FreezeAppendixVIIAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixRecoveriesDto>(HttpMethod.Post, $"api/appendix-viia/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIIAMajorHeadOptionDto>>> GetAppendixVIIAMajorHeadsAsync(string bearerToken, int demandId, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIIAMajorHeadOptionDto>>(HttpMethod.Get, $"api/appendix-viia/major-heads?demandId={demandId}", body: null, bearerToken, ct);

    // --- Appendix VII-B ---
    public Task<ApiCallResult<List<AppendixCommercialUndertakingReceiptsDto>>> GetAppendixVIIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixCommercialUndertakingReceiptsDto>>(HttpMethod.Get, $"api/appendix-viib?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> CreateAppendixVIIBAsync(string bearerToken, SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCommercialUndertakingReceiptsDto>(HttpMethod.Post, "api/appendix-viib", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> UpdateAppendixVIIBAsync(string bearerToken, int id, SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct = default) =>
        SendAsync<AppendixCommercialUndertakingReceiptsDto>(HttpMethod.Put, $"api/appendix-viib/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixVIIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-viib/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> FreezeAppendixVIIBAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixCommercialUndertakingReceiptsDto>(HttpMethod.Post, $"api/appendix-viib/{id}/freeze", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIIBSchemeOptionDto>>> GetAppendixVIIBSchemesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIIBSchemeOptionDto>>(HttpMethod.Get, "api/appendix-viib/schemes", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIIBTransactionTypeOptionDto>>> GetAppendixVIIBTransactionTypesAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIIBTransactionTypeOptionDto>>(HttpMethod.Get, "api/appendix-viib/transaction-types", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AppendixVIIBMajorHeadOptionDto>>> GetAppendixVIIBMajorHeadsAsync(string bearerToken, int demandId, int schemeId, CancellationToken ct = default) =>
        SendAsync<List<AppendixVIIBMajorHeadOptionDto>>(HttpMethod.Get, $"api/appendix-viib/major-heads?demandId={demandId}&schemeId={schemeId}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixVIIBBeActualsDto>> GetAppendixVIIBBeActualsAsync(string bearerToken, int demandId, int schemeId, int majorHeadId, string transactionType, CancellationToken ct = default) =>
        SendAsync<AppendixVIIBBeActualsDto>(HttpMethod.Get, $"api/appendix-viib/be-actuals?demandId={demandId}&schemeId={schemeId}&majorHeadId={majorHeadId}&transactionType={Uri.EscapeDataString(transactionType)}", body: null, bearerToken, ct);

    // --- Appendix X ---
    public Task<ApiCallResult<List<AppendixLoansToGovtServantsDto>>> GetAppendixXAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AppendixLoansToGovtServantsDto>>(HttpMethod.Get, $"api/appendix-x?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixLoansToGovtServantsDto>> CreateAppendixXAsync(string bearerToken, SaveAppendixLoansToGovtServantsDto request, CancellationToken ct = default) =>
        SendAsync<AppendixLoansToGovtServantsDto>(HttpMethod.Post, "api/appendix-x", request, bearerToken, ct);
    public Task<ApiCallResult<AppendixLoansToGovtServantsDto>> UpdateAppendixXAsync(string bearerToken, int id, SaveAppendixLoansToGovtServantsDto request, CancellationToken ct = default) =>
        SendAsync<AppendixLoansToGovtServantsDto>(HttpMethod.Put, $"api/appendix-x/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixXAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-x/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<AppendixLoansToGovtServantsDto>> FreezeAppendixXAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<AppendixLoansToGovtServantsDto>(HttpMethod.Post, $"api/appendix-x/{id}/freeze", body: null, bearerToken, ct);

    // --- Public Account Template ---
    public Task<ApiCallResult<List<PublicAccountReceiptPaymentDto>>> GetAppendixPAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<PublicAccountReceiptPaymentDto>>(HttpMethod.Get, $"api/appendix-pa?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<PublicAccountReceiptPaymentDto>> CreateAppendixPAAsync(string bearerToken, SavePublicAccountReceiptPaymentDto request, CancellationToken ct = default) =>
        SendAsync<PublicAccountReceiptPaymentDto>(HttpMethod.Post, "api/appendix-pa", request, bearerToken, ct);
    public Task<ApiCallResult<PublicAccountReceiptPaymentDto>> UpdateAppendixPAAsync(string bearerToken, int id, SavePublicAccountReceiptPaymentDto request, CancellationToken ct = default) =>
        SendAsync<PublicAccountReceiptPaymentDto>(HttpMethod.Put, $"api/appendix-pa/{id}", request, bearerToken, ct);
    public Task<ApiCallResult<object?>> DeleteAppendixPAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/appendix-pa/{id}", body: null, bearerToken, ct);
    public Task<ApiCallResult<PublicAccountReceiptPaymentDto>> FreezeAppendixPAAsync(string bearerToken, int id, CancellationToken ct = default) =>
        SendAsync<PublicAccountReceiptPaymentDto>(HttpMethod.Post, $"api/appendix-pa/{id}/freeze", body: null, bearerToken, ct);

    // --- FR-003: Remarks ---
    public Task<ApiCallResult<List<PreBudgetRemarkDto>>> GetRemarksAsync(string bearerToken, int demandId, int appendixId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<PreBudgetRemarkDto>>(HttpMethod.Get, $"api/remarks?demandId={demandId}&appendixId={appendixId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<PreBudgetRemarkDto>> CreateRemarkAsync(string bearerToken, CreatePreBudgetRemarkDto request, CancellationToken ct = default) =>
        SendAsync<PreBudgetRemarkDto>(HttpMethod.Post, "api/remarks", request, bearerToken, ct);

    // --- Appendix permission framework (VII-A/VII-B/X/PA-ReceiptPayment) ---
    public Task<ApiCallResult<AppendixPermissionDto>> GetAppendixPermissionsAsync(string bearerToken, string appendixCode, CancellationToken ct = default) =>
        SendAsync<AppendixPermissionDto>(HttpMethod.Get, $"api/appendix-permissions?appendixCode={Uri.EscapeDataString(appendixCode)}", body: null, bearerToken, ct);

    // --- FR-004: Autonomous Master ---
    public Task<ApiCallResult<List<AutonomousBodyDto>>> GetAutonomousBodiesAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AutonomousBodyDto>>(HttpMethod.Get, $"api/autonomous-bodies?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);
    public Task<ApiCallResult<List<AutonomousBodyDto>>> GetAutonomousBodiesForAppendixVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default) =>
        SendAsync<List<AutonomousBodyDto>>(HttpMethod.Get, $"api/autonomous-bodies/for-appendix-va?demandId={demandId}&financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AutonomousBodyDto>> CreateAutonomousBodyAsync(string bearerToken, CreateAutonomousBodyDto request, CancellationToken ct = default) =>
        SendAsync<AutonomousBodyDto>(HttpMethod.Post, "api/autonomous-bodies", request, bearerToken, ct);

    public Task<ApiCallResult<AutonomousBodyRequestDto>> RequestNewAutonomousBodyAsync(string bearerToken, CreateAutonomousBodyRequestDto request, CancellationToken ct = default) =>
        SendAsync<AutonomousBodyRequestDto>(HttpMethod.Post, "api/autonomous-bodies/requests", request, bearerToken, ct);

    public Task<ApiCallResult<List<AutonomousBodyRequestDto>>> GetPendingAutonomousBodyRequestsAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<AutonomousBodyRequestDto>>(HttpMethod.Get, "api/autonomous-bodies/requests/pending", body: null, bearerToken, ct);

    public Task<ApiCallResult<AutonomousBodyRequestDto>> ReviewAutonomousBodyRequestAsync(string bearerToken, int requestId, ReviewAutonomousBodyRequestDto review, CancellationToken ct = default) =>
        SendAsync<AutonomousBodyRequestDto>(HttpMethod.Post, $"api/autonomous-bodies/requests/{requestId}/review", review, bearerToken, ct);

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
            // Was "when (ex is not OperationCanceledException)" - excluded ALL cancellation from
            // being caught here, including the resilience pipeline's own per-attempt/total-budget
            // timeout (itself a TaskCanceledException/OperationCanceledException), not just genuine
            // caller cancellation. That meant a slow-but-not-quite-down PreBudget call could crash
            // unhandled (tester-reported 2026-08-03 for the equivalent AimClient gap) instead of
            // degrading gracefully like every other failure mode here does.
            await _logClient.ErrorAsync(
                "PreBudget call failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { relativeUrl }, ct).ConfigureAwait(false);
            return ApiCallResult<TResponse>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "The Pre-Budget service is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                // A successful response can still have an empty body - ASP.NET Core's built-in
                // HttpNoContentOutputFormatter rewrites a controller's "Ok(null)" into an empty
                // body (e.g. AppendixIController.GetByYear when there's no row for that year), and
                // ReadFromJsonAsync throws JsonException on an empty stream rather than returning
                // null. Read as a string first so a genuinely empty body just becomes default(T).
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
