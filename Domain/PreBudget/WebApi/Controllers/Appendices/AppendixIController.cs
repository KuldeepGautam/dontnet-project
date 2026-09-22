namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Appendix I: Budget and Expenditure Trends. One of 22 appendix controllers — this one (plus
/// Appendix II and III-A) is fully wired as the reference implementation of the shared
/// AppendixDataRepository&lt;TEntity&gt; pattern (design doc §10); the remaining 19 appendix tables
/// already exist (entities/EF configs/SQL) and follow this identical controller shape.
/// </summary>
[ApiController]
[Route("api/appendix-i")]
[Authorize]
public class AppendixIController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixBudgetExpenditureTrend> _repository;

    public AppendixIController(AppendixDataRepository<AppendixBudgetExpenditureTrend> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    /// <summary>
    /// Added 2026-08-03: Appendix I-A's entry form has its own "Previous Year" Revenue/Capital
    /// BE/RE columns, previously hand-typed even though that exact data already lives here, one
    /// row per year, in this appendix's own table - this lets the client pull that row directly
    /// instead of asking the user to retype figures Appendix I already has on file.
    /// </summary>
    [HttpGet("by-year")]
    public async Task<IActionResult> GetByYear([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        var record = records.FirstOrDefault();
        return Ok(record == null ? null : ToDto(record));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        // Client requirement 2026-09-09 ("add button is enabled only if current year data is not
        // available"): one row per Demand+FinancialYear, same block-any-second-record guard as
        // I-A/II - this appendix had no such check before (the old 5-year matrix form could only
        // ever submit one row per year anyway, so the gap was latent, not yet exploitable).
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear, _ => true, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"A record for Financial Year {request.FinancialYear} already exists for this Demand." });
        }

        var entity = new AppendixBudgetExpenditureTrend { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.RevenueBE,
            request.RevenueRE,
            request.RevenueActuals,
            request.RevenueActualsUptoSept,
            request.CapitalBE,
            request.CapitalRE,
            request.CapitalActuals,
            request.CapitalActualsUptoSept);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.RevenueBE,
                request.RevenueRE,
                request.RevenueActuals,
                request.RevenueActualsUptoSept,
                request.CapitalBE,
                request.CapitalRE,
                request.CapitalActuals,
                request.CapitalActualsUptoSept), ct);

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
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

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
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

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

    private static AppendixBudgetExpenditureTrendDto ToDto(AppendixBudgetExpenditureTrend e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        RevenueBE = e.RevenueBE,
        RevenueRE = e.RevenueRE,
        RevenueActuals = e.RevenueActuals,
        RevenueActualsUptoSept = e.RevenueActualsUptoSept,
        CapitalBE = e.CapitalBE,
        CapitalRE = e.CapitalRE,
        CapitalActuals = e.CapitalActuals,
        CapitalActualsUptoSept = e.CapitalActualsUptoSept,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
