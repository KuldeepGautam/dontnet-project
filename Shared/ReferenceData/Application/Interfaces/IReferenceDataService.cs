namespace UBIS.Services.ReferenceData.Application.Interfaces;

using UBIS.Services.ReferenceData.Application.DTOs;

/// <summary>
/// Read/write access to Scheme/SubScheme/MajorHead/ObjectHead — consumed directly by this
/// service's own controllers, and over HTTP by PreBudget (and any future module needing the same
/// master data) via a ReferenceDataClient, the same pattern UBIS_Web uses to call AIM.
/// </summary>
public interface IReferenceDataService
{
    Task<IReadOnlyList<SchemeDto>> GetSchemesAsync(int? demandId, CancellationToken ct);
    Task<SchemeDto?> GetSchemeAsync(int schemeId, CancellationToken ct);
    Task<SchemeDto> CreateSchemeAsync(CreateSchemeRequest request, int userId, CancellationToken ct);

    Task<IReadOnlyList<SubSchemeDto>> GetSubSchemesAsync(int? schemeId, CancellationToken ct);
    Task<SubSchemeDto> CreateSubSchemeAsync(CreateSubSchemeRequest request, int userId, CancellationToken ct);

    Task<IReadOnlyList<MajorHeadDto>> GetMajorHeadsAsync(CancellationToken ct);
    Task<MajorHeadDto> CreateMajorHeadAsync(CreateMajorHeadRequest request, int userId, CancellationToken ct);

    Task<IReadOnlyList<ObjectHeadDto>> GetObjectHeadsAsync(CancellationToken ct);
    Task<ObjectHeadDto> CreateObjectHeadAsync(CreateObjectHeadRequest request, int userId, CancellationToken ct);
}
