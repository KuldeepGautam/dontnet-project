namespace UBIS.Services.Ecl.Application.Interfaces;

using UBIS.Services.Ecl.Application.DTOs;

/// <summary>First file-upload implementation in this solution. Validates real PDF magic bytes (never trusts Content-Type/extension alone), enforces a 5 MB cap before and during buffering, and generates the stored filename itself — the caller's original filename is never used for the storage path.</summary>
public interface IEclDocumentStorage
{
    /// <summary>demandNo is the stable cross-year identifier (resolved by the caller via
    /// IEclOutlayRepository.ResolveDemandNoAsync), embedded in the stored filename per the client's
    /// naming convention: {DemandNo}_{SchemeId}_{UnixTimeMilliseconds}.pdf.</summary>
    Task<Result<EclDocumentUploadResultDto>> SaveAsync(Stream content, string? contentType, long declaredLength, string? originalFileName, int demandNo, int schemeId, CancellationToken ct);

    /// <summary>Reads back a previously-stored file for viewing. storedFileName must be exactly a
    /// value this service itself generated (validated defensively — rejects path separators/traversal
    /// even though every real caller only ever passes back a value read from dbo.ECL_T_Outlay.FileName,
    /// which this service wrote in the first place).</summary>
    Task<Result<Stream>> GetAsync(string storedFileName, CancellationToken ct);
}
