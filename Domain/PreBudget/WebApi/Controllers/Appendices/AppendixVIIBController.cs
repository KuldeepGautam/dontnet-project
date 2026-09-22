namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VII-B: Commercial receipts of departmentally-run commercial undertakings.</summary>
[ApiController]
[Route("api/appendix-viib")]
[Authorize]
public class AppendixVIIBController : ControllerBase
{
    private const string AppendixCode = "VII-B";

    private readonly AppendixDataRepository<AppendixCommercialUndertakingReceipts> _repository;
    private readonly AppendixPermissionService _permissionService;
    private readonly PreBudgetDbContext _db;

    public AppendixVIIBController(AppendixDataRepository<AppendixCommercialUndertakingReceipts> repository, AppendixPermissionService permissionService, PreBudgetDbContext db)
    {
        _repository = repository;
        _permissionService = permissionService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records.ToList(), ct));
    }

    /// <summary>
    /// Special Scheme dropdown - dbo.M_SpecialScheme rows with StmtNo="2" ("Departmental Commercial
    /// Undertakings" per dbo.M_SpecialStatement, matching VII-B's own description).
    ///
    /// NOT demand-scoped, by explicit client confirmation (2026-08-27, reverses a same-day attempt
    /// to scope it): this is deliberately a plain `select * from M_SpecialScheme` (StmtNo="2"
    /// filter only) - a Ministry can pick an Undertaking it has no prior SBEData for yet (a brand
    /// new commercial undertaking for this Demand) and fill in every figure manually; Major Head
    /// coming back empty for that case is expected, not a bug, and must not be masked by hiding the
    /// Undertaking itself from the dropdown. The earlier attempt to scope this list to only
    /// Undertakings the Demand already had SBEData for (verified correct against BIMSDemo, which
    /// really does show zero StmtNo="2" history for the specific Demand tested) solved the wrong
    /// problem - GetMajorHeads below staying demand+scheme scoped is correct and unaffected by this
    /// revert.
    /// </summary>
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes(CancellationToken ct)
    {
        var schemes = await _db.SpecialSchemes.AsNoTracking()
            .Where(s => s.StmtNo == "2")
            .OrderBy(s => s.SplSchemeName)
            .Select(s => new { schemeId = s.RowId, schemeName = s.SplSchemeName })
            .ToListAsync(ct);

        return Ok(schemes);
    }

    /// <summary>Transaction Type dropdown - dbo.Appendix_VIIB_Transaction_Type.</summary>
    [HttpGet("transaction-types")]
    public async Task<IActionResult> GetTransactionTypes(CancellationToken ct)
    {
        var types = await _db.AppendixViibTransactionTypes.AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new { id = t.Id, name = t.Name })
            .ToListAsync(ct);

        return Ok(types);
    }

    /// <summary>
    /// Major Head dropdown for the selected Demand: "select distinct MajorHeadCode from SBEData
    /// where DemandId=..." - the demand's own DemandId directly, not PrevDemandId.
    ///
    /// NOT scheme-scoped (client correction 2026-08-27, reverses the 2026-08-25 "correction" that
    /// added the Spl_SchemeId filter): verified directly against SBEData that the pre-2026-08-25
    /// behavior (Demand-only, no Scheme filter) is what actually matches the older/reference app -
    /// for Demand 1042, an unfiltered-by-scheme query returns exactly the Major Head list the
    /// client's screenshot showed (2401/2402/2435/2552/3451/3601/3602/4401/4402/4435/5475), while
    /// filtering by the selected Scheme's Spl_SchemeId returned nothing for any Scheme this Demand
    /// has. <c>schemeId</c> is still accepted (existing callers still send it) but no longer used to
    /// filter - kept only so the route/call sites don't need to change.
    /// </summary>
    [HttpGet("major-heads")]
    public async Task<IActionResult> GetMajorHeads([FromQuery] int demandId, [FromQuery] int schemeId, CancellationToken ct)
    {
        var majorHeadCodes = await _db.SbeDataRows.AsNoTracking()
            .Where(s => s.DemandId == demandId)
            .Select(s => s.MajorHeadCode)
            .Distinct()
            .ToListAsync(ct);

        var majorHeads = await _db.MajorHeads.AsNoTracking()
            .Where(m => majorHeadCodes.Contains(m.MajorHeadCode))
            .Select(m => new { majorHeadId = m.MajorHeadId, majorHeadCode = m.MajorHeadCode, majorHeadName = m.MajorHeadName })
            .ToListAsync(ct);

        return Ok(majorHeads
            .OrderBy(m => MajorHeadDisplayFormatter.NumericSortKey(m.majorHeadCode))
            .Select(m => new
            {
                m.majorHeadId,
                majorHeadCode = m.majorHeadCode,
                majorHeadLabel = MajorHeadDisplayFormatter.Format(m.majorHeadCode, m.majorHeadName)
            }));
    }

    /// <summary>
    /// BE and Actuals for the selected Special Scheme, per the client's corrected reference SQL
    /// (2026-09-14, after the prior Major-Head/Transaction-Type/PrevDemandId-for-both-sums version
    /// was confirmed live to return 0 for both figures):
    /// <c>BE = select sum(BE_Plan) from SBEData where schemeid=@schemeid and demandid=@demandid</c>
    /// (the CURRENT Demand), <c>Actuals = select sum(actual_plan) from SBEData where
    /// schemeid=@Previous_Year_SchemeID and demandid=@Previous_Year_Demand_Id</c>. "schemeid" here
    /// is Spl_SchemeId (the Special Scheme this dropdown actually selects - dbo.M_SpecialScheme has
    /// no FinancialYear column, so it isn't re-created per year and "Previous_Year_SchemeID" is
    /// simply the same schemeId value); "Previous_Year_Demand_Id" is dbo.vw_Demand.PrevDemandId for
    /// the current Demand. majorHeadId/transactionType are still accepted (existing callers still
    /// send them) but are no longer part of either sum - confirmed via live query against real
    /// SBEData rows that including them returned zero rows even where BE/Actuals genuinely exist.
    /// </summary>
    [HttpGet("be-actuals")]
    public async Task<IActionResult> GetBeActuals([FromQuery] int demandId, [FromQuery] int schemeId, [FromQuery] int majorHeadId, [FromQuery] string transactionType, CancellationToken ct)
    {
        var be = await _db.SbeDataRows.AsNoTracking()
            .Where(s => s.DemandId == demandId && s.Spl_SchemeId == schemeId)
            .SumAsync(s => (decimal?)s.BE_Plan, ct) ?? 0m;

        var prevDemandId = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.PrevDemandId)
            .FirstOrDefaultAsync(ct);

        var actuals = prevDemandId is null
            ? 0m
            : await _db.SbeDataRows.AsNoTracking()
                .Where(s => s.DemandId == prevDemandId.Value && s.Spl_SchemeId == schemeId)
                .SumAsync(s => (decimal?)s.Actual_plan, ct) ?? 0m;

        return Ok(new { be, actuals });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanCreate) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to add records to Appendix VII-B." });

        // Client report 2026-08-31: "data without filling data ... should not saved" - Major Head
        // is a required field client-side (the <select> starts `disabled` until its Scheme cascade
        // loads, which a fast/blank submit could race past), but nothing enforced it server-side,
        // so a null MajorHeadId could still be persisted regardless of what the browser validated.
        // Server-side is the real gate per this repo's convention - client-side is a convenience.
        if (request.MajorHeadId is null)
        {
            return BadRequest(new { Code = "VALIDATION_FAILED", Message = "Major Head is required." });
        }

        // Client requirement 2026-08-27: "uniqueness for Departmental Commercial Undertaking - Major
        // Head - Type of Transaction combination."
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.MajorHeadId == request.MajorHeadId
                     && string.Equals(e.TransactionType, request.TransactionType, StringComparison.OrdinalIgnoreCase),
                excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Undertaking/Major Head/Type of Transaction combination already exists for this Demand." });
        }

        var entity = new AppendixCommercialUndertakingReceipts { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.SchemeId,
            request.TransactionType,
            request.MajorHeadId,
            request.ActualsY2,
            request.ActualsY1,
            request.ActualsUptoSept,
            request.ActualsUptoSeptPrevYear,
            request.BE,
            request.RE,
            request.IncreasedBE,
            request.NBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanEdit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to edit Appendix VII-B records." });

        if (request.MajorHeadId is null)
        {
            return BadRequest(new { Code = "VALIDATION_FAILED", Message = "Major Head is required." });
        }

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.MajorHeadId == request.MajorHeadId
                     && string.Equals(e.TransactionType, request.TransactionType, StringComparison.OrdinalIgnoreCase),
                excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Undertaking/Major Head/Type of Transaction combination already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.SchemeId,
                request.TransactionType,
                request.MajorHeadId,
                request.ActualsY2,
                request.ActualsY1,
                request.ActualsUptoSept,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.RE,
                request.IncreasedBE,
                request.NBE), ct);
            return Ok(await ToDtoAsync(updated, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "UPDATE_FAILED", Message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanDelete) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to delete Appendix VII-B records." });

        try
        {
            await _repository.DeleteAsync(id, userId.Value, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "DELETE_FAILED", Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/freeze")]
    public async Task<IActionResult> FreezeRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanSubmit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to freeze Appendix VII-B records." });

        try
        {
            var frozen = await _repository.FreezeAsync(id, userId.Value, ct);
            return Ok(await ToDtoAsync(frozen, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    private async Task<AppendixCommercialUndertakingReceiptsDto> ToDtoAsync(AppendixCommercialUndertakingReceipts e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixCommercialUndertakingReceipts> { e }, ct);
        return dtos[0];
    }

    // Batches the Scheme/Major Head name lookups into 2 queries total regardless of record count,
    // instead of an N+1 per-row lookup - same pattern as Appendix III/V-B. The grid shows
    // Department Name/Major Head Code per the client's reference screenshot (2026-08-07), not the
    // raw ids the earlier version returned.
    private async Task<List<AppendixCommercialUndertakingReceiptsDto>> ToDtosAsync(List<AppendixCommercialUndertakingReceipts> entities, CancellationToken ct)
    {
        var schemeIds = entities.Where(e => e.SchemeId.HasValue).Select(e => e.SchemeId!.Value).Distinct().ToList();
        var majorHeadIds = entities.Where(e => e.MajorHeadId.HasValue).Select(e => e.MajorHeadId!.Value).Distinct().ToList();

        var schemeNames = await _db.SpecialSchemes
            .Where(s => schemeIds.Contains(s.RowId))
            .ToDictionaryAsync(s => s.RowId, s => s.SplSchemeName, ct);

        var majorHeads = await _db.MajorHeads
            .Where(m => majorHeadIds.Contains(m.MajorHeadId))
            .ToDictionaryAsync(m => m.MajorHeadId, m => m, ct);

        return entities.Select(e => new AppendixCommercialUndertakingReceiptsDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            SchemeId = e.SchemeId,
            SchemeName = e.SchemeId.HasValue && schemeNames.TryGetValue(e.SchemeId.Value, out var sn) ? sn : null,
            TransactionType = e.TransactionType,
            MajorHeadId = e.MajorHeadId,
            MajorHeadCode = e.MajorHeadId.HasValue && majorHeads.TryGetValue(e.MajorHeadId.Value, out var mh)
                ? MajorHeadDisplayFormatter.Format(mh.MajorHeadCode, mh.MajorHeadName)
                : null,
            ActualsY2 = e.ActualsY2,
            ActualsY1 = e.ActualsY1,
            ActualsUptoSept = e.ActualsUptoSept,
            ActualsUptoSeptPrevYear = e.ActualsUptoSeptPrevYear,
            BE = e.BE,
            RE = e.RE,
            IncreasedBE = e.IncreasedBE,
            NBE = e.NBE,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
