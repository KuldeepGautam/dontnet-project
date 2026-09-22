namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix V-B: Establishment Expenditure - Object Head wise. Maps to dbo.Appendix_VB_EstablishmentByObjectHead (was legacy Temp_EstExp_ObjHeadwise).</summary>
[ApiController]
[Route("api/appendix-vb")]
[Authorize]
public class AppendixVBController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixEstablishmentByObjectHead> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVBController(AppendixDataRepository<AppendixEstablishmentByObjectHead> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    /// <summary>
    /// Object Head dropdown, scoped to the selected Demand (client testing feedback, 2026-08-25 -
    /// "Object Head is not bound, bind it based on demand") - previously returned every active
    /// Object Head globally regardless of which Demand was selected. Displayed as "{ObjectHeadCode}
    /// - {ObjectHeadName}" ordered numerically by ObjectHeadCode (matches the same Code-based
    /// convention as Major Head, see MajorHeadDisplayFormatter).
    ///
    /// Scoping source: dbo.DDG (see DdgNew's own doc comment) already carries a real per-Demand
    /// Object-Head relationship - GetPreviousYearBe below already matches its HeadOfAccount's last 2
    /// characters against the selected Object Head's code, scheme rows only (SchemeId not null/0).
    /// Reused here as the "which Object Heads exist for this Demand" source rather than inventing a
    /// second mechanism. Verified directly against the DB: 62 active Object Heads total, only 39
    /// distinct codes have DDG rows for Demand 1042 - a meaningfully narrower, real per-Demand list.
    /// </summary>
    [HttpGet("object-heads")]
    public async Task<IActionResult> GetObjectHeads([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var demandObjectHeadCodes = await _db.DdgNewRows.AsNoTracking()
            .Where(d => d.DemandId == demandId && d.SchemeId != null && d.SchemeId != 0)
            .Select(d => d.HeadOfAccount)
            .Distinct()
            .ToListAsync(ct);

        var objectHeads = await _db.ObjectHeads
            .Where(o => o.IsActive && !o.IsDeleted)
            .Select(o => new { o.ObjectHeadId, o.ObjectHeadCode, o.ObjectHeadName })
            .ToListAsync(ct);

        var scoped = objectHeads
            .Where(o => !string.IsNullOrEmpty(o.ObjectHeadCode) && demandObjectHeadCodes.Any(h => h.EndsWith(o.ObjectHeadCode)))
            .ToList();

        return Ok(scoped
            .OrderBy(o => MajorHeadDisplayFormatter.NumericSortKey(o.ObjectHeadCode))
            .Select(o => new { o.ObjectHeadId, Label = $"{o.ObjectHeadCode} - {o.ObjectHeadName}" }));
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        var dtos = await ToDtosAsync(records.ToList(), financialYear, ct);
        // Client testing feedback (2026-08-24): the grid was showing rows in insertion order
        // (effectively by the saved record's own Id), not the Object Head's own id - sort by
        // that instead so the grid reads in ascending Object Head id order.
        return Ok(dtos.OrderBy(d => MajorHeadDisplayFormatter.NumericSortKey(d.ObjectHeadCode)));
    }

    /// <summary>
    /// BE and Actuals pre-fill on Object Head change, per the client's reference SQL (2026-08-07 for
    /// BE, 2026-08-10 for Actuals - final correction). BE resolves PrevDemandId via M_Demand and
    /// sums dbo.DDG.NBE_Plan for that prior demand. Actuals sums dbo.DDG.Actual_Plan against
    /// the CURRENT DemandId directly - NOT PrevDemandId - per the client's explicit correction:
    /// "Actual is 2 year behind current financial year ... so not require previous year demandid"
    /// (the Actual_Plan column already holds the FY-2 figure within the current year's DDG rows).
    /// Both queries match the selected Object Head's code against the last 2 characters of
    /// HeadOfAccount (legacy RIGHT(HeadofAccount,2) = @ObjectHeadCode - ObjectHeadCode is a fixed
    /// 2-char code, so EndsWith is exactly equivalent), exclude rows with no SchemeId (legacy
    /// isnull(schemeid,0)!=0), and scale /10000 rounded to 2 decimals (legacy
    /// FORMAT(SUM(...)/10000.0,'0.##')). Verified directly against the DB for DemandId=1042, Object
    /// Heads 01/02/07: Actuals returns 221.41/4.89/185.62, matching the client's screenshot exactly.
    /// </summary>
    [HttpGet("previous-year-be")]
    public async Task<IActionResult> GetPreviousYearBe([FromQuery] int demandId, [FromQuery] int objectHeadId, CancellationToken ct)
    {
        var prevDemandId = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.PrevDemandId)
            .FirstOrDefaultAsync(ct);

        var objectHeadCode = await _db.ObjectHeads.AsNoTracking()
            .Where(o => o.ObjectHeadId == objectHeadId)
            .Select(o => o.ObjectHeadCode)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(objectHeadCode))
        {
            return Ok(new { be = 0m, actuals = 0m });
        }

        var be = prevDemandId is null
            ? 0m
            : await _db.DdgNewRows.AsNoTracking()
                .Where(d => d.DemandId == prevDemandId.Value
                    && d.SchemeId != null && d.SchemeId != 0
                    && d.HeadOfAccount.EndsWith(objectHeadCode))
                .SumAsync(d => (decimal?)d.NBE_Plan, ct) ?? 0m;

        var actuals = await _db.DdgNewRows.AsNoTracking()
            .Where(d => d.DemandId == demandId
                && d.SchemeId != null && d.SchemeId != 0
                && d.HeadOfAccount.EndsWith(objectHeadCode))
            .SumAsync(d => (decimal?)d.Actual_Plan, ct) ?? 0m;

        return Ok(new { be = Math.Round(be / 10000m, 2), actuals = Math.Round(actuals / 10000m, 2) });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "there should not be duplicate values based on Object Head
        // based on Demand" - same uniqueness convention as Appendix VI-C/V-A's AutonomousBodyId.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.ObjectHeadId == request.ObjectHeadId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Object Head already exists for this Demand." });
        }

        var entity = new AppendixEstablishmentByObjectHead { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.ObjectHeadId,
            request.Actuals,
            request.ActualsUptoSeptPrevYear,
            request.BE,
            request.ActualsUptoSept,
            request.ProposedRE,
            request.ProposedNBE,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, request.FinancialYear, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.ObjectHeadId == request.ObjectHeadId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Object Head already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.ObjectHeadId,
                request.Actuals,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedRE,
                request.ProposedNBE,
                request.Remarks), ct);
            return Ok(await ToDtoAsync(updated, request.FinancialYear, ct));
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
            return Ok(await ToDtoAsync(frozen, frozen.FinancialYear, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    private async Task<AppendixEstablishmentByObjectHeadDto> ToDtoAsync(AppendixEstablishmentByObjectHead e, string financialYear, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixEstablishmentByObjectHead> { e }, financialYear, ct);
        return dtos[0];
    }

    private async Task<List<AppendixEstablishmentByObjectHeadDto>> ToDtosAsync(List<AppendixEstablishmentByObjectHead> entities, string financialYear, CancellationToken ct)
    {
        var objectHeadIds = entities.Where(e => e.ObjectHeadId.HasValue).Select(e => e.ObjectHeadId!.Value).Distinct().ToList();
        var objectHeads = await _db.ObjectHeads
            .Where(o => objectHeadIds.Contains(o.ObjectHeadId))
            .ToDictionaryAsync(o => o.ObjectHeadId, o => o, ct);

        return entities.Select(e =>
        {
            objectHeads.TryGetValue(e.ObjectHeadId ?? 0, out var head);
            return new AppendixEstablishmentByObjectHeadDto
            {
                Id = e.Id,
                DemandId = e.DemandId,
                FinancialYear = e.FinancialYear,
                ObjectHeadId = e.ObjectHeadId,
                ObjectHeadCode = head?.ObjectHeadCode,
                ObjectHeadName = head != null ? $"{head.ObjectHeadCode} - {head.ObjectHeadName}" : null,
                Actuals = e.Actuals,
                ActualsUptoSeptPrevYear = e.ActualsUptoSeptPrevYear,
                BE = e.BE,
                ActualsUptoSept = e.ActualsUptoSept,
                ProposedRE = e.ProposedRE,
                ProposedNBE = e.ProposedNBE,
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
