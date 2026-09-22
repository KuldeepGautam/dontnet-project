namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-B: Pending Liabilities of Ministries. Maps to dbo.Appendix_VIB_PendingLiabilities (was legacy Temp_PendingLiabilities).</summary>
[ApiController]
[Route("api/appendix-vib")]
[Authorize]
public class AppendixVIBController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixPendingLiabilities> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIBController(AppendixDataRepository<AppendixPendingLiabilities> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    /// <summary>
    /// Category dropdown (added 2026-07-30 - missing from the original design pass; the approved
    /// design's real "Select Category -> Select Scheme -> Select SubScheme" cascade needs it). Real
    /// dbo.M_Category rows, same reference table Appendix III uses - unlike III's synthetic 4-value
    /// "Balance Type", here it's a plain, generic Scheme classification with no special-casing.
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string financialYear, CancellationToken ct)
    {
        var categories = await _db.Categories
            .Where(c => c.IsActive && c.FinancialYear == financialYear)
            .Select(c => new { c.CategoryId, c.SerialNo, c.CategoryName })
            .ToListAsync(ct);

        return Ok(categories
            .OrderBy(c => c.SerialNo)
            .Select(c => new { c.CategoryId, CategoryName = $"{c.SerialNo}-{c.CategoryName}" }));
    }

    /// <summary>Scheme dropdown for the demand, cascading from the selected Category (added 2026-07-30).</summary>
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes([FromQuery] int demandId, [FromQuery] int categoryId, CancellationToken ct)
    {
        var schemes = await _db.Schemes
            .Where(s => s.DemandId == demandId && s.CategoryId == categoryId && s.IsActive && !s.IsDeleted)
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName })
            .ToListAsync(ct);

        // Client requirement 2026-08-27: "same issue with scheme dropdown in the appendix" - same
        // fix as the grid's SchemeName below, plus sorted numerically by SchemeSrNo (matching every
        // other appendix's Scheme dropdown, e.g. AppendixIVController.GetSchemes) instead of by the
        // display string as text.
        return Ok(schemes
            .OrderBy(s => s.SchemeSrNo ?? 0)
            .Select(s => new { s.SchemeId, SchemeName = SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName) }));
    }

    /// <summary>SubScheme dropdown, cascading from the selected Scheme. Label is
    /// "{SchemeSrNo}.{SubSchemeSrNo:00}-{SubSchemeName}" (client testing feedback, 2026-08-24 -
    /// "should have serial no like 1.01, SchemeSrNo.SubSchemeSrNo appended with 0 when &lt;10"),
    /// sorted numerically by SchemeSrNo then SubSchemeSrNo, not by the display string as text.</summary>
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
    /// BE auto-load (client reference SQL, 2026-08-25): "select sum(BE_Plan) from SBEData where
    /// DemandId=@demandId and CategoryId=@categoryId and SchemeID=@schemeId" - SchemeID here is the
    /// plain M_Scheme id (not Spl_SchemeId, the Special Scheme VII-A/VII-B/PA use).
    /// </summary>
    [HttpGet("be")]
    public async Task<IActionResult> GetBe([FromQuery] int demandId, [FromQuery] int categoryId, [FromQuery] int schemeId, CancellationToken ct)
    {
        var be = await _db.SbeDataRows.AsNoTracking()
            .Where(s => s.DemandId == demandId && s.CategoryId == categoryId && s.SchemeID == schemeId)
            .SumAsync(s => (decimal?)s.BE_Plan, ct);

        return Ok(new { Be = be ?? 0m });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records.ToList(), ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixPendingLiabilitiesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var validationError = await ValidateRequiredSelectionsAsync(request, ct);
        if (validationError != null)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = validationError });
        }

        // Client requirement 2026-08-27: "no duplicate allowed for Category, Scheme and Subscheme."
        // Category isn't a stored column on this table (see ToDtosAsync's comment - it's always
        // derived from the chosen Scheme's own CategoryId), so a Scheme+SubScheme match already
        // guarantees Category uniqueness too.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Category/Scheme/Sub-Scheme combination already exists for this Demand." });
        }

        var entity = new AppendixPendingLiabilities { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.SchemeId,
            request.SubSchemeId,
            request.PendingLiabilityAsOnMarch31,
            request.BE,
            request.EstimatedExpenditure,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixPendingLiabilitiesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var validationError = await ValidateRequiredSelectionsAsync(request, ct);
        if (validationError != null)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = validationError });
        }

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Category/Scheme/Sub-Scheme combination already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.SchemeId,
                request.SubSchemeId,
                request.PendingLiabilityAsOnMarch31,
                request.BE,
                request.EstimatedExpenditure,
                request.Remarks), ct);
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

    private async Task<AppendixPendingLiabilitiesDto> ToDtoAsync(AppendixPendingLiabilities e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixPendingLiabilities> { e }, ct);
        return dtos[0];
    }

    /// <summary>
    /// Client requirement 2026-08-27: "category, scheme and sub-scheme are required fields so
    /// update validations." Category itself isn't a stored field on this DTO (see the class-level
    /// comment on ToDtosAsync), so there's nothing to check for it here - it's already `required`
    /// on the entry form's own Category select, which the Scheme/SubScheme cascade can't even reach
    /// without. Scheme is unconditionally required. SubScheme is required only when the selected
    /// Scheme actually has at least one active SubScheme - many Schemes structurally have none (see
    /// GetSubSchemes/every other appendix's own SubScheme dropdown), so making it unconditionally
    /// required would block a valid save for any Scheme that just doesn't have sub-schemes.
    /// </summary>
    private async Task<string?> ValidateRequiredSelectionsAsync(SaveAppendixPendingLiabilitiesDto request, CancellationToken ct)
    {
        if (request.SchemeId is null)
        {
            return "Select Scheme.";
        }

        var schemeHasSubSchemes = await _db.SubSchemes
            .AnyAsync(s => s.SchemeId == request.SchemeId.Value && s.IsActive && !s.IsDeleted, ct);

        if (schemeHasSubSchemes && request.SubSchemeId is null)
        {
            return "Select Sub-Scheme.";
        }

        return null;
    }

    private async Task<List<AppendixPendingLiabilitiesDto>> ToDtosAsync(List<AppendixPendingLiabilities> entities, CancellationToken ct)
    {
        var schemeIds = entities.Where(e => e.SchemeId.HasValue).Select(e => e.SchemeId!.Value).Distinct().ToList();
        var subSchemeIds = entities.Where(e => e.SubSchemeId.HasValue).Select(e => e.SubSchemeId!.Value).Distinct().ToList();

        // Category isn't a stored column on this table (the legacy sp_Temp_PendingLiabilities never
        // had one either - only SchemeID/SubSchemeID) - it's derived from the chosen Scheme's own
        // CategoryId purely so "Edit" can re-select the right Category without an extra round trip.
        var schemes = await _db.Schemes.Where(s => schemeIds.Contains(s.SchemeId)).ToDictionaryAsync(s => s.SchemeId, s => s, ct);
        var subSchemes = await _db.SubSchemes.Where(s => subSchemeIds.Contains(s.SubSchemeId)).ToDictionaryAsync(s => s.SubSchemeId, s => s, ct);

        var categoryIds = schemes.Values.Where(s => s.CategoryId.HasValue).Select(s => s.CategoryId!.Value).Distinct().ToList();
        // Client requirement 2026-08-27: "add categorysrno before categoryname in grid" - matches
        // GetCategories' own dropdown format ("{SerialNo}-{CategoryName}") exactly, which this grid
        // previously didn't (plain CategoryName only).
        var categoryNames = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToDictionaryAsync(c => c.CategoryId, c => $"{c.SerialNo}-{c.CategoryName}", ct);

        return entities.Select(e =>
        {
            var scheme = e.SchemeId.HasValue && schemes.TryGetValue(e.SchemeId.Value, out var s) ? s : null;
            var categoryId = scheme?.CategoryId;

            return new AppendixPendingLiabilitiesDto
            {
                Id = e.Id,
                DemandId = e.DemandId,
                FinancialYear = e.FinancialYear,
                CategoryId = categoryId,
                CategoryName = categoryId.HasValue && categoryNames.TryGetValue(categoryId.Value, out var cn) ? cn : null,
                SchemeId = e.SchemeId,
                // Client requirement 2026-08-27: "Scheme Sr No missing before scheme name in grid" -
                // was plain scheme?.SchemeName; every other appendix's grid uses SchemeDisplayFormatter
                // ("{SchemeSrNo} - {SchemeName}"), including this same row's own SubSchemeName below.
                SchemeName = scheme != null ? SchemeDisplayFormatter.Format(scheme.SchemeSrNo, scheme.SchemeName) : null,
                SubSchemeId = e.SubSchemeId,
                SubSchemeName = e.SubSchemeId.HasValue && subSchemes.TryGetValue(e.SubSchemeId.Value, out var subScheme)
                    ? SubSchemeDisplayFormatter.Format(scheme?.SchemeSrNo, subScheme.SubSchemeSrNo, subScheme.SubSchemeName)
                    : null,
                PendingLiabilityAsOnMarch31 = e.PendingLiabilityAsOnMarch31,
                BE = e.BE,
                EstimatedExpenditure = e.EstimatedExpenditure,
                Remarks = e.Remarks,
                IsFrozen = e.IsFrozen
            };
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
