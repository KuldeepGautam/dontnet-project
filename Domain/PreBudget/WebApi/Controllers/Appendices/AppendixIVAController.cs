namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix IV-A: Estimates under Special Component Plan for Scheduled Castes (Minor Head 789).</summary>
[ApiController]
[Route("api/appendix-iva")]
[Authorize]
public class AppendixIVAController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixScspExpenditure> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixIVAController(AppendixDataRepository<AppendixScspExpenditure> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records.ToList(), ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixScspExpenditureDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // "Without selecting the subscheme clicking on the submit button is successfully save the
        // record" / "an additional entry is created in the grid" / "Multiple entry is submitted on
        // the same Scheme" (client testing feedback, 2026-08-25, reported on the IV-A/IV-B pair) -
        // one entry per Scheme+SubScheme per Demand/FinancialYear, same uniqueness convention as
        // Appendix VII-A's Major Head + Scheme Name check.
        if (await HasDuplicateAsync(request, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Scheme and Sub-Scheme." });
        }

        var entity = new AppendixScspExpenditure { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
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
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixScspExpenditureDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await HasDuplicateAsync(request, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Scheme and Sub-Scheme." });
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

    private async Task<bool> HasDuplicateAsync(SaveAppendixScspExpenditureDto request, int? excludeId, CancellationToken ct)
    {
        return await _db.AppendixIVAScspExpenditures.AsNoTracking().AnyAsync(e =>
            !e.IsDeleted
            && e.DemandId == request.DemandId
            && e.FinancialYear == request.FinancialYear
            && e.SchemeId == request.SchemeId
            && e.SubSchemeId == request.SubSchemeId
            && (excludeId == null || e.Id != excludeId.Value), ct);
    }

    private async Task<AppendixScspExpenditureDto> ToDtoAsync(AppendixScspExpenditure e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixScspExpenditure> { e }, ct);
        return dtos[0];
    }

    /// <summary>Batches the Scheme/SubScheme name lookups into 2 queries total (same pattern as Appendix III/IV) - the grid shows names, not raw ids (client review 2026-08-05).</summary>
    private async Task<List<AppendixScspExpenditureDto>> ToDtosAsync(List<AppendixScspExpenditure> entities, CancellationToken ct)
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

        // Same Category resolution as AppendixIVController.ToDtosAsync (added there 2026-09-18,
        // mirrored here 2026-09-21) - Appendix IV-A has no Category selection at data-entry time
        // either, so this is resolved purely for the structured export.
        var categoryIds = schemes.Where(s => s.CategoryId.HasValue).Select(s => s.CategoryId!.Value).Distinct().ToList();
        var categoriesById = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.CategoryName, c.SerialNo })
            .ToDictionaryAsync(c => c.CategoryId, ct);
        var categoryTypeBySchemeId = schemes.ToDictionary(
            s => s.SchemeId,
            s => s.CategoryId.HasValue && categoriesById.TryGetValue(s.CategoryId.Value, out var c) ? c.CategoryName : null);
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

        return entities.Select(e => new AppendixScspExpenditureDto
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
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
