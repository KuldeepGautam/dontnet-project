namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Appendix III: CNA/SNA Balances of Schemes — CategoryType covers all 4 real "Balance Type"
/// variants the legacy system (BIMSDemo) used in one table: 2 map to real dbo.M_Category rows
/// ("Central Sector Schemes/Projects", "Centrally Sponsored Schemes"), 2 are the CNA/SNA exemption
/// split which was never a real M_Category row in the legacy system either (see
/// Previous_Dem_Sch_Cat_SubSchId/CNASNAForExemption stored procedures, reviewed 2026-07-29).
/// </summary>
[ApiController]
[Route("api/appendix-iii")]
[Authorize]
public class AppendixIIIController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixCnaSnaBalance> _repository;
    private readonly PreBudgetDbContext _db;

    // Must match CK_Appendix_III_CnaSnaBalance_CategoryType (DBA-owned constraint on
    // dbo.Appendix_III_CnaSnaBalance, widened 2026-07-29 to 5 values - these 4 plus the legacy
    // generic "ExemptedFromCnaSna" kept only for pre-existing rows, never offered as a new
    // selection) - validated here so an invalid value gets a clean 400 instead of an unhandled
    // SqlException/500 from the CHECK constraint.
    private static readonly (string Value, string Label)[] AllowedCategoryTypes =
    [
        ("CentralSectorScheme", "Central Sector Schemes/Projects"),
        ("CentrallySponsoredScheme", "Centrally Sponsored Schemes"),
        ("ExemptedFromCna", "Schemes exempted from CNA"),
        ("ExemptedFromSna", "Schemes exempted from SNA")
    ];

    public AppendixIIIController(AppendixDataRepository<AppendixCnaSnaBalance> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    /// <summary>Balance Type dropdown options - fixed list, not DB-driven (see class doc comment).</summary>
    [HttpGet("categories")]
    public IActionResult GetCategories() =>
        Ok(AllowedCategoryTypes.Select(c => new { value = c.Value, label = c.Label }));

    /// <summary>
    /// Scheme dropdown, cascading from the selected Demand + Balance Type. Client review 2026-08-06
    /// - matches the client's own reference queries against BIMSDemo:
    /// "select * from M_scheme where demandid=@d and categoryid in (select CategoryId from
    /// M_category where FinancialYear=@fy and CategoryName like '%...%') order by SchemeSrNo"
    /// - CategoryName matched via Contains (not exact equality) and scoped to the current FY
    /// (M_Category is year-scoped; the previous version didn't filter on FinancialYear at all, so a
    /// same-named Category row from a different year could leak in).
    ///
    /// Balance Type -> M_Category mapping (client instruction 2026-09-10):
    ///   Central Sector Schemes/Projects  -> "Central Sector Schemes/Projects"
    ///   Centrally Sponsored Schemes      -> "Centrally Sponsored Schemes"
    ///   Schemes exempted from CNA        -> "Central Sector Schemes/Projects"
    ///   Schemes exempted from SNA        -> "Centrally Sponsored Schemes"
    /// i.e. the two CNA/SNA exemption types load the same Scheme list as their parent category
    /// (replaces the earlier curated dbo.Appendix_III_CnaSnaBalance lookup for this dropdown).
    /// </summary>
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes([FromQuery] int demandId, [FromQuery] string categoryType, [FromQuery] string financialYear, CancellationToken ct)
    {
        var currentFyCategories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.FinancialYear == financialYear)
            .Select(c => new { c.CategoryId, c.CategoryName })
            .ToListAsync(ct);

        var nameFragment = categoryType switch
        {
            "CentralSectorScheme" or "ExemptedFromCna" => "Central Sector Schemes/Projects",
            "CentrallySponsoredScheme" or "ExemptedFromSna" => "Centrally Sponsored Schemes",
            _ => null
        };
        if (nameFragment is null)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "Unknown Balance Type." });
        }

        // currentFyCategories is already materialized (in-memory List), so this LINQ-to-objects
        // Contains is a plain substring check - fine to use StringComparison here, unlike a
        // LINQ-to-entities query where that overload wouldn't translate to SQL.
        var categoryIds = currentFyCategories
            .Where(c => c.CategoryName != null && c.CategoryName.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.CategoryId)
            .ToList();

        var schemeIds = await _db.Schemes
            .Where(s => s.DemandId == demandId && s.IsActive && !s.IsDeleted && s.CategoryId != null && categoryIds.Contains(s.CategoryId!.Value))
            .Select(s => s.SchemeId)
            .ToListAsync(ct);

        var schemes = await _db.Schemes
            .Where(s => schemeIds.Contains(s.SchemeId))
            .OrderBy(s => s.SchemeSrNo ?? 0)
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName })
            .ToListAsync(ct);

        return Ok(schemes.Select(s => new { s.SchemeId, SchemeName = SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName) }));
    }

    /// <summary>SubScheme dropdown, cascading from the selected Scheme. Label is
    /// "{SchemeSrNo}.{SubSchemeSrNo:00} - {SubSchemeName}" via <see cref="SubSchemeDisplayFormatter"/>
    /// - the single shared format used by every appendix's SubScheme dropdown (client testing
    /// feedback, 2026-08-24), sorted numerically by SubSchemeSrNo.</summary>
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
    /// Equivalent of legacy stored procedure Previous_Dem_Sch_Cat_SubSchId's NBETOTAL query: resolves
    /// the prior-year Demand/Category/Scheme/SubScheme via each master table's Prev*Id lineage
    /// column, then sums NBE_PLan (now pre-aggregated in SbeNbeSummary) for that prior-year
    /// combination. Returns 0 when any link in the chain is missing (matches the legacy proc's own
    /// ISNULL(..., 0) behaviour) rather than failing - a scheme/demand simply may not have existed
    /// the previous year. Not surfaced on the approved Appendix III page yet; available for the UI
    /// to call once a "previous year" reference field is added to the design.
    /// </summary>
    [HttpGet("previous-year-nbe-total")]
    public async Task<IActionResult> GetPreviousYearNbeTotal([FromQuery] int demandId, [FromQuery] int schemeId, [FromQuery] int? subSchemeId, CancellationToken ct)
    {
        var scheme = await _db.Schemes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SchemeId == schemeId && s.DemandId == demandId, ct);
        if (scheme?.PrevSchemeId is null || scheme.CategoryId is null)
        {
            return Ok(new { nbeTotal = 0m });
        }

        var prevDemandId = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.PrevDemandId)
            .FirstOrDefaultAsync(ct);

        var prevCategoryId = await _db.Categories.AsNoTracking()
            .Where(c => c.CategoryId == scheme.CategoryId)
            .Select(c => c.PrevCategoryId)
            .FirstOrDefaultAsync(ct);

        if (prevDemandId is null || prevCategoryId is null)
        {
            return Ok(new { nbeTotal = 0m });
        }

        var query = _db.SbeNbeSummaries.AsNoTracking()
            .Where(x => x.DemandId == prevDemandId.Value && x.CategoryId == prevCategoryId.Value && x.SchemeId == scheme.PrevSchemeId.Value);

        if (subSchemeId.HasValue)
        {
            var prevSubSchemeId = await _db.SubSchemes.AsNoTracking()
                .Where(ss => ss.SubSchemeId == subSchemeId.Value)
                .Select(ss => ss.PrevSubschemeId)
                .FirstOrDefaultAsync(ct);

            if (prevSubSchemeId is null)
            {
                return Ok(new { nbeTotal = 0m });
            }

            query = query.Where(x => x.SubSchemeId == prevSubSchemeId.Value);
        }

        var nbeTotal = await query.SumAsync(x => (decimal?)x.NbeTotal, ct) ?? 0m;
        return Ok(new { nbeTotal });
    }

    /// <summary>
    /// BE auto-load. Bug fix 2026-09-15 (client report + DB verification: "BE 2025-2026" showed 0
    /// for a scheme that has real SBE data): this summed NbeTotal from SbeNbeSummaryByYear (a
    /// pre-aggregation of SBEData's NBE_Plan/NBE_NonPlan columns) - but "BE" here means the actual
    /// Budget Estimate figure, not the Next-year Budget Estimate. Switched to sum BE_Plan straight
    /// from the raw SBEData table, same source/pattern as Appendix VI-B's own BE auto-load
    /// (AppendixVIBController.GetBe: "select sum(BE_Plan) from SBEData where DemandId=... and
    /// SchemeID=..."), keyed by DemandId (already year-scoped per dbo.vw_Demand - a different
    /// DemandId per FinancialYear for the same Demand No.) + SchemeID, not a separate
    /// FinancialYear filter.
    /// </summary>
    [HttpGet("be-by-scheme")]
    public async Task<IActionResult> GetBeByScheme([FromQuery] int demandId, [FromQuery] int schemeId, CancellationToken ct)
    {
        var be = await _db.SbeDataRows.AsNoTracking()
            .Where(x => x.DemandId == demandId && x.SchemeID == schemeId)
            .SumAsync(x => (decimal?)x.BE_Plan, ct) ?? 0m;

        return Ok(new { Be = be });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records.ToList(), ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixCnaSnaBalanceDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-31: "validation unique rows for balance type, scheme and
        // sub-scheme, no duplicate entries allowed" - server-side is the real gate per this
        // repo's convention (client-side is a convenience). SubSchemeId can legitimately be null
        // (a Scheme with no Sub-Schemes), so two nulls must still compare equal here.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => string.Equals(e.CategoryType, request.CategoryType, StringComparison.OrdinalIgnoreCase)
                     && e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId,
                excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Balance Type/Scheme/Sub-Scheme combination already exists for this Demand." });
        }

        var entity = new AppendixCnaSnaBalance { DemandId = request.DemandId, FinancialYear = request.FinancialYear };

        try
        {
            entity.UpdateFrom(
                request.CategoryType,
                request.SchemeId,
                request.SubSchemeId,
                request.BE,
                request.BalanceAsOnAprilOpening,
                request.ReleasesDuringFY,
                request.BalanceAsOnSeptClosing,
                request.DateOfLastRelease,
                request.AmountOfLastRelease,
                request.NotTransferredToSnaAsOnSept,
                request.Remarks,
                request.ReasonForExemption);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
        }

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixCnaSnaBalanceDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => string.Equals(e.CategoryType, request.CategoryType, StringComparison.OrdinalIgnoreCase)
                     && e.SchemeId == request.SchemeId && e.SubSchemeId == request.SubSchemeId,
                excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Balance Type/Scheme/Sub-Scheme combination already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.CategoryType,
                request.SchemeId,
                request.SubSchemeId,
                request.BE,
                request.BalanceAsOnAprilOpening,
                request.ReleasesDuringFY,
                request.BalanceAsOnSeptClosing,
                request.DateOfLastRelease,
                request.AmountOfLastRelease,
                request.NotTransferredToSnaAsOnSept,
                request.Remarks,
                request.ReasonForExemption), ct);
            return Ok(await ToDtoAsync(updated, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
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

    private async Task<AppendixCnaSnaBalanceDto> ToDtoAsync(AppendixCnaSnaBalance e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixCnaSnaBalance> { e }, ct);
        return dtos[0];
    }

    // Batches the Scheme/SubScheme name lookups into 2 queries total regardless of record count,
    // instead of an N+1 per-row lookup - the grid on Appendix III shows Scheme Name/Sub-Scheme
    // Name per the approved design, not the raw ids the earlier version returned.
    private async Task<List<AppendixCnaSnaBalanceDto>> ToDtosAsync(List<AppendixCnaSnaBalance> entities, CancellationToken ct)
    {
        var schemeIds = entities.Where(e => e.SchemeId.HasValue).Select(e => e.SchemeId!.Value).Distinct().ToList();
        var subSchemeIds = entities.Where(e => e.SubSchemeId.HasValue).Select(e => e.SubSchemeId!.Value).Distinct().ToList();

        var schemes = await _db.Schemes
            .Where(s => schemeIds.Contains(s.SchemeId))
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName })
            .ToListAsync(ct);
        var schemeNames = schemes.ToDictionary(s => s.SchemeId, s => SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName));
        var schemeNamesRaw = schemes.ToDictionary(s => s.SchemeId, s => s.SchemeName);
        var schemeSrNoById = schemes.ToDictionary(s => s.SchemeId, s => s.SchemeSrNo);

        var subSchemeRows = await _db.SubSchemes
            .Where(s => subSchemeIds.Contains(s.SubSchemeId))
            .Select(s => new { s.SubSchemeId, s.SchemeId, s.SubSchemeSrNo, s.SubSchemeName })
            .ToListAsync(ct);
        var subSchemeNames = subSchemeRows.ToDictionary(
            s => s.SubSchemeId,
            s => SubSchemeDisplayFormatter.Format(schemeSrNoById.TryGetValue(s.SchemeId, out var srn) ? srn : 0, s.SubSchemeSrNo, s.SubSchemeName));
        var subSchemeNamesRaw = subSchemeRows.ToDictionary(s => s.SubSchemeId, s => s.SubSchemeName);
        var subSchemeSrNoById = subSchemeRows.ToDictionary(s => s.SubSchemeId, s => s.SubSchemeSrNo);

        return entities.Select(e => new AppendixCnaSnaBalanceDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            CategoryType = e.CategoryType,
            SchemeId = e.SchemeId,
            SchemeName = e.SchemeId.HasValue && schemeNames.TryGetValue(e.SchemeId.Value, out var sn) ? sn : null,
            SubSchemeId = e.SubSchemeId,
            SubSchemeName = e.SubSchemeId.HasValue && subSchemeNames.TryGetValue(e.SubSchemeId.Value, out var ssn) ? ssn : null,
            SchemeSrNo = e.SchemeId.HasValue && schemeSrNoById.TryGetValue(e.SchemeId.Value, out var schemeSrNo) ? schemeSrNo : null,
            SchemeNameRaw = e.SchemeId.HasValue && schemeNamesRaw.TryGetValue(e.SchemeId.Value, out var schemeNameRaw) ? schemeNameRaw : null,
            SubSchemeNameRaw = e.SubSchemeId.HasValue && subSchemeNamesRaw.TryGetValue(e.SubSchemeId.Value, out var subSchemeNameRaw) ? subSchemeNameRaw : null,
            SubSchemeSrNo = e.SubSchemeId.HasValue && subSchemeSrNoById.TryGetValue(e.SubSchemeId.Value, out var subSchemeSrNo) ? subSchemeSrNo : null,
            BE = e.BE,
            BalanceAsOnAprilOpening = e.BalanceAsOnAprilOpening,
            ReleasesDuringFY = e.ReleasesDuringFY,
            BalanceAsOnSeptClosing = e.BalanceAsOnSeptClosing,
            DateOfLastRelease = e.DateOfLastRelease,
            AmountOfLastRelease = e.AmountOfLastRelease,
            NotTransferredToSnaAsOnSept = e.NotTransferredToSnaAsOnSept,
            Remarks = e.Remarks,
            ReasonForExemption = e.ReasonForExemption,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
