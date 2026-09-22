namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix IV: Estimates of Schemes. Maps to dbo.Appendix_IV_EstimatesOfSchemes (was legacy Temp_EstimatesofSchemes).</summary>
[ApiController]
[Route("api/appendix-iv")]
[Authorize]
public class AppendixIVController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixEstimatesOfSchemes> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixIVController(AppendixDataRepository<AppendixEstimatesOfSchemes> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    /// <summary>
    /// Scheme dropdown for the demand - no Balance Type split here (that's an Appendix III concept
    /// only). Also backs Appendix IV-A/IV-B's Scheme dropdowns (they call this same endpoint via
    /// GetAppendixIVSchemesAsync - Scheme is shared reference data, not appendix-specific). Label
    /// is always ISNULL(SchemeSrNo,0)-SchemeName per the approved design (e.g. "39-National Mission
    /// on Natural Farming", or "0-Some Scheme" when SchemeSrNo is NULL - was already NULL for ~25%
    /// of rows in the legacy source). Sorted ascending by the raw numeric SchemeSrNo column itself
    /// (2026-08-04 - was briefly sorted by the display string instead, corrected back).
    /// </summary>
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes([FromQuery] int demandId, CancellationToken ct)
    {
        var schemes = await _db.Schemes
            .Where(s => s.DemandId == demandId && s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.SchemeSrNo ?? 0)
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName })
            .ToListAsync(ct);

        return Ok(schemes.Select(s => new { s.SchemeId, SchemeName = SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName) }));
    }

    /// <summary>SubScheme dropdown, cascading from the selected Scheme. Label is
    /// "{SchemeSrNo}.{SubSchemeSrNo:00} - {SubSchemeName}" via <see cref="SubSchemeDisplayFormatter"/>
    /// - the single shared format used by every appendix's SubScheme dropdown (client testing
    /// feedback, 2026-08-24: "use single point of reference for subscheme, for all appendixes"),
    /// sorted numerically by SubSchemeSrNo, not by the display string as text.</summary>
    [HttpGet("subschemes")]
    public async Task<IActionResult> GetSubSchemes([FromQuery] int schemeId, CancellationToken ct)
    {
        var schemeSrNo = await _db.Schemes
            .Where(s => s.SchemeId == schemeId)
            .Select(s => s.SchemeSrNo ?? 0)
            .FirstOrDefaultAsync(ct);

        var subSchemes = await _db.SubSchemes
            .Where(s => s.SchemeId == schemeId && s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.SubSchemeSrNo ?? 0)
            .Select(s => new { s.SubSchemeId, s.SubSchemeSrNo, s.SubSchemeName })
            .ToListAsync(ct);

        return Ok(subSchemes.Select(s => new
        {
            s.SubSchemeId,
            SubSchemeName = SubSchemeDisplayFormatter.Format(schemeSrNo, s.SubSchemeSrNo, s.SubSchemeName)
        }));
    }

    /// <summary>
    /// BE auto-load (client review 2026-08-05): "select sum(NBE_plan) from SBEData where
    /// FinancialYear = @FY and SchemeID = @SchemeId" - or, when a SubScheme is selected, "sum of
    /// subschemeid" instead (client's own wording), same SbeNbeSummaryByYear aggregate Appendix III
    /// uses.
    /// </summary>
    [HttpGet("be-by-scheme")]
    public async Task<IActionResult> GetBeByScheme([FromQuery] int demandId, [FromQuery] int schemeId, [FromQuery] int? subSchemeId, [FromQuery] string financialYear, CancellationToken ct)
    {
        // Default: sum across every SubScheme under this Scheme. Once a SubScheme is picked, narrow
        // to just that one (client's "in case sub scheme is present, we will use sum of subschemeid").
        var query = _db.SbeNbeSummariesByYear.AsNoTracking()
            .Where(x => x.DemandId == demandId && x.SchemeId == schemeId && x.FinancialYear == financialYear);

        if (subSchemeId.HasValue)
        {
            query = query.Where(x => x.SubSchemeId == subSchemeId.Value);
        }

        var be = await query.SumAsync(x => (decimal?)x.NbeTotal, ct) ?? 0m;
        return Ok(new { Be = be });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        // "grid sorting order by schemesrno, subschemesrno" (client testing feedback, 2026-08-25) -
        // sorts the underlying entities before formatting, not the formatted DTOs (their SchemeName/
        // SubSchemeName are display strings like "4 - Foo"/"4.03 - Bar" by this point, not safe to
        // sort as text - see SchemeDisplayFormatter/SubSchemeDisplayFormatter).
        var schemeIds = records.Where(e => e.SchemeId.HasValue).Select(e => e.SchemeId!.Value).Distinct().ToList();
        var subSchemeIds = records.Where(e => e.SubSchemeId.HasValue).Select(e => e.SubSchemeId!.Value).Distinct().ToList();
        var schemeSrNoById = await _db.Schemes.Where(s => schemeIds.Contains(s.SchemeId))
            .ToDictionaryAsync(s => s.SchemeId, s => s.SchemeSrNo ?? 0, ct);
        var subSchemeSrNoById = await _db.SubSchemes.Where(s => subSchemeIds.Contains(s.SubSchemeId))
            .ToDictionaryAsync(s => s.SubSchemeId, s => s.SubSchemeSrNo ?? 0, ct);

        var sorted = records
            .OrderBy(e => e.SchemeId.HasValue && schemeSrNoById.TryGetValue(e.SchemeId.Value, out var ssn) ? ssn : 0)
            .ThenBy(e => e.SubSchemeId.HasValue && subSchemeSrNoById.TryGetValue(e.SubSchemeId.Value, out var subsn) ? subsn : 0)
            .ToList();

        return Ok(await ToDtosAsync(sorted, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "Unique row entry for Demand and Scheme, if there is
        // sub-scheme, then uniqueness validation for Demand - Scheme - Subscheme." UBIS_Web hides
        // the Save button client-side once a matching combination is already saved (see
        // AppendixIV.cshtml/pre-budget-meeting.js), but that's UI convenience, not the guard - this
        // is the authoritative check, same as every other server-side rule in this codebase.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Scheme" + (request.SubSchemeId.HasValue ? "/Sub-Scheme" : "") + " already exists for this Demand." });
        }

        var entity = new AppendixEstimatesOfSchemes { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.SchemeId,
            request.SubSchemeId,
            request.Actuals,
            request.ActualsUptoSeptPrevYear,
            request.BE,
            request.ActualsUptoSept,
            request.ProposedRE,
            request.ProposedNBE,
            request.RemarksMinistry,
            request.RemarksBudget,
            request.BudgetRecommendedRE,
            request.BudgetRecommendedNBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Scheme" + (request.SubSchemeId.HasValue ? "/Sub-Scheme" : "") + " already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.SchemeId,
                request.SubSchemeId,
                request.Actuals,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedRE,
                request.ProposedNBE,
                request.RemarksMinistry,
                request.RemarksBudget,
                request.BudgetRecommendedRE,
                request.BudgetRecommendedNBE), ct);
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

    private async Task<AppendixEstimatesOfSchemesDto> ToDtoAsync(AppendixEstimatesOfSchemes e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixEstimatesOfSchemes> { e }, ct);
        return dtos[0];
    }

    // Batches Scheme/SubScheme name lookups into 2 queries regardless of record count (same
    // pattern as AppendixIIIController) - the grid shows Scheme Name/Sub-Scheme Name per the
    // approved design, not raw ids.
    private async Task<List<AppendixEstimatesOfSchemesDto>> ToDtosAsync(List<AppendixEstimatesOfSchemes> entities, CancellationToken ct)
    {
        var schemeIds = entities.Where(e => e.SchemeId.HasValue).Select(e => e.SchemeId!.Value).Distinct().ToList();
        var subSchemeIds = entities.Where(e => e.SubSchemeId.HasValue).Select(e => e.SubSchemeId!.Value).Distinct().ToList();

        var schemes = await _db.Schemes
            .Where(s => schemeIds.Contains(s.SchemeId))
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName, s.CategoryId })
            .ToListAsync(ct);
        var schemeNames = schemes.ToDictionary(s => s.SchemeId, s => SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName));
        var schemeNamesRaw = schemes.ToDictionary(s => s.SchemeId, s => s.SchemeName);
        var schemeSrNoById = schemes.ToDictionary(s => s.SchemeId, s => s.SchemeSrNo);

        // Client requirement 2026-09-18: the export groups rows by the Scheme's own Category
        // (Centrally Sponsored Schemes / Central Sector Schemes, same M_Category reference data
        // Appendix III/III-B already resolve) - Appendix IV itself has no Balance-Type/Category
        // selection at data-entry time (see GetSchemes' own doc comment), so this is resolved here
        // purely for the export, not stored on the entity.
        var categoryIds = schemes.Where(s => s.CategoryId.HasValue).Select(s => s.CategoryId!.Value).Distinct().ToList();
        var categoriesById = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.CategoryName, c.SerialNo })
            .ToDictionaryAsync(c => c.CategoryId, ct);
        var categoryTypeBySchemeId = schemes.ToDictionary(
            s => s.SchemeId,
            s => s.CategoryId.HasValue && categoriesById.TryGetValue(s.CategoryId.Value, out var c) ? c.CategoryName : null);
        // Added 2026-09-21 alongside SchemeSrNo below, for the Budget Division export's Category/
        // Scheme ordering - see AppendixEstimatesOfSchemesDto.CategorySerialNo's own doc comment.
        var categorySerialNoBySchemeId = schemes.ToDictionary(
            s => s.SchemeId,
            s => s.CategoryId.HasValue && categoriesById.TryGetValue(s.CategoryId.Value, out var c) ? c.SerialNo : null);

        var subSchemeRows = await _db.SubSchemes
            .Where(s => subSchemeIds.Contains(s.SubSchemeId))
            .Select(s => new { s.SubSchemeId, s.SchemeId, s.SubSchemeSrNo, s.SubSchemeName })
            .ToListAsync(ct);
        var subSchemeNames = subSchemeRows.ToDictionary(
            s => s.SubSchemeId,
            s => SubSchemeDisplayFormatter.Format(schemeSrNoById.TryGetValue(s.SchemeId, out var srn) ? srn : 0, s.SubSchemeSrNo, s.SubSchemeName));
        var subSchemeNamesRaw = subSchemeRows.ToDictionary(s => s.SubSchemeId, s => s.SubSchemeName);
        var subSchemeSrNoById = subSchemeRows.ToDictionary(s => s.SubSchemeId, s => s.SubSchemeSrNo);

        return entities.Select(e => new AppendixEstimatesOfSchemesDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            SchemeId = e.SchemeId,
            SchemeName = e.SchemeId.HasValue && schemeNames.TryGetValue(e.SchemeId.Value, out var sn) ? sn : null,
            SubSchemeId = e.SubSchemeId,
            SubSchemeName = e.SubSchemeId.HasValue && subSchemeNames.TryGetValue(e.SubSchemeId.Value, out var ssn) ? ssn : null,
            CategoryType = e.SchemeId.HasValue && categoryTypeBySchemeId.TryGetValue(e.SchemeId.Value, out var categoryType) ? categoryType : null,
            CategorySerialNo = e.SchemeId.HasValue && categorySerialNoBySchemeId.TryGetValue(e.SchemeId.Value, out var categorySerialNo) ? categorySerialNo : null,
            SchemeSrNo = e.SchemeId.HasValue && schemeSrNoById.TryGetValue(e.SchemeId.Value, out var schemeSrNo) ? schemeSrNo : null,
            SchemeNameRaw = e.SchemeId.HasValue && schemeNamesRaw.TryGetValue(e.SchemeId.Value, out var schemeNameRaw) ? schemeNameRaw : null,
            SubSchemeNameRaw = e.SubSchemeId.HasValue && subSchemeNamesRaw.TryGetValue(e.SubSchemeId.Value, out var subSchemeNameRaw) ? subSchemeNameRaw : null,
            SubSchemeSrNo = e.SubSchemeId.HasValue && subSchemeSrNoById.TryGetValue(e.SubSchemeId.Value, out var subSchemeSrNo) ? subSchemeSrNo : null,
            Actuals = e.Actuals,
            ActualsUptoSeptPrevYear = e.ActualsUptoSeptPrevYear,
            BE = e.BE,
            ActualsUptoSept = e.ActualsUptoSept,
            ProposedRE = e.ProposedRE,
            AddlReSought = e.AddlReSought,
            BudgetRecommendedRE = e.BudgetRecommendedRE,
            ProposedNBE = e.ProposedNBE,
            AddlNbeSought = e.AddlNbeSought,
            BudgetRecommendedNBE = e.BudgetRecommendedNBE,
            RemarksMinistry = e.RemarksMinistry,
            RemarksBudget = e.RemarksBudget,
            PercentWrtBE = e.PercentWrtBE,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
