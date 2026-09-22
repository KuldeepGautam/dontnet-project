namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix I-A: Projected Demand by Ministry.</summary>
[ApiController]
[Route("api/appendix-ia")]
[Authorize]
public class AppendixIAController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixProjectedDemand> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixIAController(AppendixDataRepository<AppendixProjectedDemand> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    /// <summary>
    /// Previous year's Revenue/Capital BE, per the client's reference SQL (2026-08-07): resolves
    /// PrevDemandId via M_Demand (a new DemandId is minted every FY for the same logical Demand),
    /// then sums dbo.SBEData.NBE_PLan for that prior demand+year, split Revenue (MajorHeadCode &lt;
    /// 4000) vs Capital (&gt;= 4000). Replaces the earlier self-referential version that read
    /// Appendix I's own saved row instead (wrong per this reference SQL - re-derives from the raw
    /// SBEData source like the legacy system did, not from whatever the user already typed into
    /// Appendix I). MajorHeadCode is a fixed 4-digit varchar (leading zeros) so lexicographic
    /// string comparison against "4000" matches the legacy numeric comparison exactly.
    /// </summary>
    [HttpGet("previous-year-be")]
    public async Task<IActionResult> GetPreviousYearBe([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var prevDemandId = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.PrevDemandId)
            .FirstOrDefaultAsync(ct);

        if (prevDemandId is null)
        {
            return Ok(new { found = false });
        }

        var rows = _db.SbeDataRows.AsNoTracking()
            .Where(s => s.DemandId == prevDemandId.Value && s.FinancialYear == financialYear);

        var revenueBE = await rows
            .Where(s => s.MajorHeadCode.CompareTo("4000") < 0)
            .SumAsync(s => (decimal?)s.NBE_PLan, ct) ?? 0m;

        var capitalBE = await rows
            .Where(s => s.MajorHeadCode.CompareTo("4000") >= 0)
            .SumAsync(s => (decimal?)s.NBE_PLan, ct) ?? 0m;

        return Ok(new { found = true, revenueBE, capitalBE, totalBE = revenueBE + capitalBE });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixProjectedDemandDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "data can be edited if there is entry, second entry is
        // prohibited" - Appendix I-A is one row per Demand+FinancialYear (the entry form always
        // edits that single row once it exists), so any second Create for the same Demand+FY is a
        // duplicate. UBIS_Web enriches this generic message with the Demand's display name (not
        // available in this service's own M_Demand mapping) before showing it to the user.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear, _ => true, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Demand already exists." });
        }

        var entity = new AppendixProjectedDemand { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.PrevYrMinRevenueBE,
            request.PrevYrMinRevenueRE,
            request.PrevYrMinCapitalBE,
            request.PrevYrMinCapitalRE,
            request.CurrYrMinRevenueBE,
            request.CurrYrMinCapitalBE,
            request.CurrYrMinMtefRevenueBE,
            request.CurrYrMinMtefCapitalBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixProjectedDemandDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.PrevYrMinRevenueBE,
                request.PrevYrMinRevenueRE,
                request.PrevYrMinCapitalBE,
                request.PrevYrMinCapitalRE,
                request.CurrYrMinRevenueBE,
                request.CurrYrMinCapitalBE,
                request.CurrYrMinMtefRevenueBE,
                request.CurrYrMinMtefCapitalBE), ct);
            return Ok(ToDto(updated));
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
            return Ok(ToDto(frozen));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    private static AppendixProjectedDemandDto ToDto(AppendixProjectedDemand e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        PrevYrMinRevenueBE = e.PrevYrMinRevenueBE,
        PrevYrMinRevenueRE = e.PrevYrMinRevenueRE,
        PrevYrMinCapitalBE = e.PrevYrMinCapitalBE,
        PrevYrMinCapitalRE = e.PrevYrMinCapitalRE,
        CurrYrMinRevenueBE = e.CurrYrMinRevenueBE,
        CurrYrMinCapitalBE = e.CurrYrMinCapitalBE,
        CurrYrMinMtefRevenueBE = e.CurrYrMinMtefRevenueBE,
        CurrYrMinMtefCapitalBE = e.CurrYrMinMtefCapitalBE,
        TotalBE = e.TotalBE,
        PrevYrTotalBE = e.PrevYrTotalBE,
        PrevYrTotalRE = e.PrevYrTotalRE,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
