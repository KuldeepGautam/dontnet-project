namespace UBIS.Services.Ecl.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.Ecl.Application.DTOs;
using UBIS.Services.Ecl.Application.Interfaces;
using UBIS.Services.Ecl.Domain.Entities;
using UBIS.Services.Ecl.Infrastructure.Security;

/// <summary>
/// Thin controller — all business logic (state machine invariants, percentage recompute, audit
/// stamping) lives in EclSchemeOutlay/EclOutlayRepository, not here. Covers CRUD, submit-for-
/// approval, record-actuals, and the category/scheme/authority/financial-year lookup endpoints.
/// Approve/reject/reapproval-grant live in EclApprovalController instead (DOE-only).
/// </summary>
[ApiController]
[Route("api/ecl/outlay")]
[Authorize]
public class EclOutlayController : ControllerBase
{
    private readonly IEclOutlayRepository _repository;
    private readonly IEclDocumentStorage _documentStorage;

    public EclOutlayController(IEclOutlayRepository repository, IEclDocumentStorage documentStorage)
    {
        _repository = repository;
        _documentStorage = documentStorage;
    }

    [HttpGet]
    public async Task<IActionResult> GetOutlays([FromQuery] int? demandId, [FromQuery] int? categoryId, [FromQuery] int? schemeId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandCategorySchemeYearAsync(demandId, categoryId, schemeId, financialYear, ct);
        var schemeIds = records.Where(r => r.SchemeId.HasValue).Select(r => r.SchemeId!.Value).Distinct();
        var demandIds = records.Where(r => r.DemandId.HasValue).Select(r => r.DemandId!.Value).Distinct();
        var schemeInfo = await _repository.GetSchemeInfoByIdsAsync(schemeIds, ct);
        var demandInfo = await _repository.GetDemandInfoByIdsAsync(demandIds, ct);
        return Ok(records.Select(r => ToDto(r, schemeInfo, demandInfo)));
    }

    [HttpGet("{rowId:int}")]
    public async Task<IActionResult> GetOutlay(int rowId, CancellationToken ct)
    {
        var record = await _repository.GetByIdAsync(rowId, ct);
        if (record is null)
        {
            return NotFound(new { Code = "NOT_FOUND", Message = $"Outlay {rowId} not found." });
        }

        var schemeInfo = record.SchemeId.HasValue
            ? await _repository.GetSchemeInfoByIdsAsync(new[] { record.SchemeId.Value }, ct)
            : new Dictionary<int, (string, int?)>();
        var demandInfo = record.DemandId.HasValue
            ? await _repository.GetDemandInfoByIdsAsync(new[] { record.DemandId.Value }, ct)
            : new Dictionary<int, (int?, string?)>();
        return Ok(ToDto(record, schemeInfo, demandInfo));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOutlay([FromBody] SaveSchemeOutlayRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var approvalAuthorityId = await _repository.EnsureApprovalAuthorityAsync(request.ApproveAuth, userId.Value, ct);
        var appraisalAuthorityId = await _repository.EnsureAppraiseAuthorityAsync(request.AppraisalAuth, userId.Value, ct);

        var entity = FromRequest(new EclSchemeOutlay(), request, approvalAuthorityId, appraisalAuthorityId);
        var result = await _repository.AddAsync(entity, userId.Value, ct);

        return result.IsSuccess ? Ok(ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    [HttpPut("{rowId:int}")]
    public async Task<IActionResult> UpdateOutlay(int rowId, [FromBody] SaveSchemeOutlayRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var approvalAuthorityId = await _repository.EnsureApprovalAuthorityAsync(request.ApproveAuth, userId.Value, ct);
        var appraisalAuthorityId = await _repository.EnsureAppraiseAuthorityAsync(request.AppraisalAuth, userId.Value, ct);

        var result = await _repository.UpdateAsync(rowId, userId.Value, entity => FromRequest(entity, request, approvalAuthorityId, appraisalAuthorityId), ct);

        return result.IsSuccess ? Ok(ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    [HttpDelete("{rowId:int}")]
    public async Task<IActionResult> DeleteOutlay(int rowId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.DeleteAsync(rowId, userId.Value, ct);

        return result.IsSuccess ? NoContent() : ToActionResult(result.Error!);
    }

    [HttpPost("{rowId:int}/submit-for-approval")]
    public async Task<IActionResult> SubmitForApproval(int rowId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.SubmitForApprovalAsync(rowId, userId.Value, ct);

        return result.IsSuccess ? Ok(ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    [HttpPost("{rowId:int}/actuals")]
    public async Task<IActionResult> RecordActuals(int rowId, [FromBody] EclActualsRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.RecordActualsAsync(rowId, request.Actuals, userId.Value, ct);

        return result.IsSuccess ? Ok(ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    /// <summary>Uploads the supporting PDF for an outlay. Max 5 MB, enforced both by RequestSizeLimit here and again inside IEclDocumentStorage while streaming. demandId is resolved to its stable DemandNo before being handed to storage, since that's what the stored filename embeds.</summary>
    [HttpPost("documents")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    // IFormFile is bound from form data automatically - no [FromForm] needed (and Swashbuckle can't
    // generate a schema for the [FromForm]+IFormFile combination specifically, which was breaking
    // /swagger/v1/swagger.json for the whole service with a 500 until this was removed).
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] int demandId, [FromForm] int schemeId, CancellationToken ct)
    {
        var demandNo = await _repository.ResolveDemandNoAsync(demandId, ct);

        await using var stream = file.OpenReadStream();
        var result = await _documentStorage.SaveAsync(stream, file.ContentType, file.Length, file.FileName, demandNo, schemeId, ct);

        return result.IsSuccess ? Ok(result.Data) : ToActionResult(result.Error!);
    }

    /// <summary>Streams back a previously-uploaded PDF for viewing (not download — Content-Disposition: inline, matching the client's "open in popup" requirement). Looks the file up by RowId rather than trusting a raw filename from the caller, since a stored filename alone carries no authorization context.</summary>
    [HttpGet("{rowId:int}/document")]
    public async Task<IActionResult> GetDocument(int rowId, CancellationToken ct)
    {
        var record = await _repository.GetByIdAsync(rowId, ct);
        if (record is null || string.IsNullOrEmpty(record.FileName))
        {
            return NotFound(new { Code = "NOT_FOUND", Message = $"Outlay {rowId} has no attached document." });
        }

        var result = await _documentStorage.GetAsync(record.FileName, ct);
        if (!result.IsSuccess)
        {
            return ToActionResult(result.Error!);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{record.FileName}\"";
        return File(result.Data!, "application/pdf");
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string financialYear, CancellationToken ct)
    {
        var categories = await _repository.GetCategoriesAsync(financialYear, ct);
        return Ok(categories.Select(c => new EclCategoryDto { CategoryId = c.CategoryId, SerialNo = c.SerialNo, CategoryName = c.CategoryName }));
    }

    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes([FromQuery] int demandId, [FromQuery] int categoryId, CancellationToken ct)
    {
        var schemes = await _repository.GetSchemesAsync(demandId, categoryId, ct);
        return Ok(schemes.Select(s => new EclSchemeDto { SchemeId = s.SchemeId, DemandId = s.DemandId, SchemeName = s.SchemeName, CategoryId = s.CategoryId, SchemeSrNo = s.SchemeSrNo, UmbSchemeId = s.UmbSchemeId }));
    }

    /// <summary>Umbrella-scheme options for ECL Master's "Add Schemes" screen — see
    /// EclOutlayRepository.GetUmbrellaSchemeOptionsAsync's doc comment for the exact
    /// client-specified query this implements (dbo.M_UmbScheme, CategoryId + DemandId match).</summary>
    [HttpGet("umbrella-schemes")]
    public async Task<IActionResult> GetUmbrellaSchemes([FromQuery] int categoryId, [FromQuery] int demandId, CancellationToken ct)
    {
        var schemes = await _repository.GetUmbrellaSchemeOptionsAsync(categoryId, demandId, ct);
        return Ok(schemes.Select(u => new EclUmbSchemeDto { UmbSchemeId = u.UmbSchemeId, UmSchemeName = u.UmSchemeName, DisplaySeqNo = u.DisplaySeqNo }));
    }

    /// <summary>Creates a new dbo.M_Scheme row for the ECL Master "Add Schemes" screen (added 2026-08-18, closing a gap flagged when that screen was first built).</summary>
    [HttpPost("schemes")]
    public async Task<IActionResult> CreateScheme([FromBody] CreateSchemeRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.CreateSchemeAsync(request, userId.Value, ct);
        if (!result.IsSuccess)
        {
            return ToActionResult(result.Error!);
        }

        var scheme = result.Data!;
        return Ok(new EclSchemeDto { SchemeId = scheme.SchemeId, DemandId = scheme.DemandId, SchemeName = scheme.SchemeName, CategoryId = scheme.CategoryId, SchemeSrNo = scheme.SchemeSrNo, UmbSchemeId = scheme.UmbSchemeId });
    }

    [HttpGet("authorities")]
    public async Task<IActionResult> GetApprovalAuthorities(CancellationToken ct)
    {
        var authorities = await _repository.GetApprovalAuthoritiesAsync(ct);
        return Ok(authorities.Select(a => new EclApprovalAuthorityDto { AuthorityId = a.AuthorityId, AuthorityName = a.AuthorityName, DisplaySequenceNo = a.DisplaySequenceNo }));
    }

    /// <summary>Independent from GetApprovalAuthorities above — see EclAppraiseAuthority's doc comment for why these are two separate master tables (client request 2026-08-25).</summary>
    [HttpGet("appraise-authorities")]
    public async Task<IActionResult> GetAppraiseAuthorities(CancellationToken ct)
    {
        var authorities = await _repository.GetAppraiseAuthoritiesAsync(ct);
        return Ok(authorities.Select(a => new EclAppraiseAuthorityDto { AppraiseId = a.AppraiseId, AppraiseName = a.AppraiseName, DisplaySequenceNo = a.DisplaySequenceNo }));
    }

    /// <summary>Source switched 2026-08-19 from dbo.ECL_Config to dbo.M_FinanceCommission (client
    /// request) — see EclFinanceCommission's doc comment. Falls back to the old ECL_Config source
    /// only if no M_FinanceCommission row exists yet, so this doesn't hard-break before the new
    /// table is populated in a given environment.</summary>
    [HttpGet("financial-years")]
    public async Task<IActionResult> GetFinancialYears(CancellationToken ct)
    {
        var financeCommission = await _repository.GetFinanceCommissionAsync(ct);
        List<string> years;
        if (financeCommission != null)
        {
            years = financeCommission.GenerateSelectableFinancialYears();
        }
        else
        {
            var config = await _repository.GetConfigAsync(ct);
            years = config?.GenerateSelectableFinancialYears() ?? new List<string>();
        }

        return Ok(new EclFinancialYearOptionsDto { FinancialYears = years });
    }

    private static EclSchemeOutlay FromRequest(EclSchemeOutlay entity, SaveSchemeOutlayRequestDto request, int? approvalAuthorityId, int? appraisalAuthorityId)
    {
        entity.FinancialYear = request.FinancialYear;
        entity.CategoryId = request.CategoryId;
        entity.SubCategoryId = request.SubCategoryId;
        entity.DemandId = request.DemandId;
        entity.SchemeId = request.SchemeId;
        entity.ApproveAuth = request.ApproveAuth;
        entity.ApprovalAuthorityId = approvalAuthorityId;
        entity.TotalOutlay = request.TotalOutlay;
        entity.CentralShare = request.CentralShare;

        var outlay = request.Outlay ?? new decimal?[10];
        entity.Fy1Outlay = outlay.ElementAtOrDefault(0);
        entity.Fy2Outlay = outlay.ElementAtOrDefault(1);
        entity.Fy3Outlay = outlay.ElementAtOrDefault(2);
        entity.Fy4Outlay = outlay.ElementAtOrDefault(3);
        entity.Fy5Outlay = outlay.ElementAtOrDefault(4);
        entity.Fy6Outlay = outlay.ElementAtOrDefault(5);
        entity.Fy7Outlay = outlay.ElementAtOrDefault(6);
        entity.Fy8Outlay = outlay.ElementAtOrDefault(7);
        entity.Fy9Outlay = outlay.ElementAtOrDefault(8);
        entity.Fy10Outlay = outlay.ElementAtOrDefault(9);

        entity.UserRemarks = request.UserRemarks;
        entity.Is16Fc = request.Is16Fc;
        entity.AppraisalAuth = request.AppraisalAuth;
        entity.AppraisalAuthorityId = appraisalAuthorityId;
        entity.WhetherAppraised = request.WhetherAppraised;
        entity.AppraisalStatusRemarks = request.AppraisalStatusRemarks;
        entity.IsApproved = request.IsApproved;
        entity.NotApprovedRem = request.NotApprovedRem;
        entity.SchemeEndYear = request.SchemeEndYear;
        if (!string.IsNullOrEmpty(request.FileName))
        {
            entity.FileName = request.FileName;
        }

        return entity;
    }

    /// <summary>schemeNames is optional — write endpoints (Create/Update/SubmitForApproval/etc.)
    /// don't pass it since UBIS_Web re-fetches the whole list (which does resolve names) right
    /// after any save; only GetOutlays/GetOutlay populate it for a genuinely accurate response.</summary>
    internal static EclSchemeOutlayDto ToDto(
        EclSchemeOutlay e,
        IReadOnlyDictionary<int, (string SchemeName, int? SchemeSrNo)>? schemeInfo = null,
        IReadOnlyDictionary<int, (int? DemandNo, string? DemandName)>? demandInfo = null)
    {
        var (schemeName, schemeSrNo) = e.SchemeId.HasValue && schemeInfo != null && schemeInfo.TryGetValue(e.SchemeId.Value, out var si) ? si : (null, null);
        var (demandNo, demandName) = e.DemandId.HasValue && demandInfo != null && demandInfo.TryGetValue(e.DemandId.Value, out var di) ? di : (null, null);

        return new()
        {
        RowId = e.RowId,
        FinancialYear = e.FinancialYear,
        CategoryId = e.CategoryId,
        SubCategoryId = e.SubCategoryId,
        DemandId = e.DemandId,
        SchemeId = e.SchemeId,
        SchemeName = schemeName,
        SchemeSrNo = schemeSrNo,
        DemandNo = demandNo,
        DemandName = demandName,
        ApproveAuth = e.ApproveAuth,
        ApprovalAuthorityId = e.ApprovalAuthorityId,
        TotalOutlay = e.TotalOutlay,
        CentralSharePercentage = e.CentralSharePercentage,
        CentralShare = e.CentralShare,
        Outlay = new[] { e.Fy1Outlay, e.Fy2Outlay, e.Fy3Outlay, e.Fy4Outlay, e.Fy5Outlay, e.Fy6Outlay, e.Fy7Outlay, e.Fy8Outlay, e.Fy9Outlay, e.Fy10Outlay },
        FileName = e.FileName,
        UserRemarks = e.UserRemarks,
        SendToDoe = e.SendToDoe,
        ApprovedByDoe = e.ApprovedByDoe,
        DoeApprovalStatusDisplay = e.DoeApprovalStatusDisplay,
        ReapprovalByDoe = e.ReapprovalByDoe,
        ApproveUserId = e.ApproveUserId,
        ApproveDate = e.ApproveDate,
        DoeRemarks = e.DoeRemarks,
        Actuals = new[] { e.Fy1Actuals, e.Fy2Actuals, e.Fy3Actuals, e.Fy4Actuals, e.Fy5Actuals, e.Fy6Actuals, e.Fy7Actuals, e.Fy8Actuals, e.Fy9Actuals, e.Fy10Actuals },
        ActualsEntryDate = e.ActualsEntryDate,
        PrevRowId = e.PrevRowId,
        Is16Fc = e.Is16Fc,
        AppraisalAuth = e.AppraisalAuth,
        AppraisalAuthorityId = e.AppraisalAuthorityId,
        WhetherAppraised = e.WhetherAppraised,
        AppraisalStatusRemarks = e.AppraisalStatusRemarks,
        IsApproved = e.IsApproved,
        NotApprovedRem = e.NotApprovedRem,
        SchemeEndYear = e.SchemeEndYear
        };
    }

    private IActionResult ToActionResult(Error error) => new ObjectResult(error) { StatusCode = MapStatusCode(error.Code) };

    private static int MapStatusCode(string code) => code switch
    {
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
