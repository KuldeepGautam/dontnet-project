namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-C: Details of Corpus Funds. Requires an Autonomous Body (FR-004) selection.</summary>
[ApiController]
[Route("api/appendix-vic")]
[Authorize]
public class AppendixVICController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixCorpusFund> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVICController(AppendixDataRepository<AppendixCorpusFund> repository, PreBudgetDbContext db)
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
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixCorpusFundDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "uniqueness for demand - autonomous body."
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: null, ct))
        {
            var abName = await _db.AutonomousBodies.Where(a => a.AutonomousBodyId == request.AutonomousBodyId).Select(a => a.Name).FirstOrDefaultAsync(ct);
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Autonomous Body '{abName ?? request.AutonomousBodyId.ToString()}' already exists for this Demand." });
        }

        var entity = new AppendixCorpusFund { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.AutonomousBodyId,
            request.IsPublicAccount,
            request.AccumulatedBalancePrevYear,
            request.AccumulatedBalance,
            request.ActualExpenditureY1,
            request.ActualExpenditureY2,
            request.ActualExpenditureY3,
            request.AllocationInBE,
            request.ExpenditureTillSept,
            request.ReasonForCorpusFund);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixCorpusFundDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: id, ct))
        {
            var abName = await _db.AutonomousBodies.Where(a => a.AutonomousBodyId == request.AutonomousBodyId).Select(a => a.Name).FirstOrDefaultAsync(ct);
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Autonomous Body '{abName ?? request.AutonomousBodyId.ToString()}' already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.AutonomousBodyId,
                request.IsPublicAccount,
                request.AccumulatedBalancePrevYear,
                request.AccumulatedBalance,
                request.ActualExpenditureY1,
                request.ActualExpenditureY2,
                request.ActualExpenditureY3,
                request.AllocationInBE,
                request.ExpenditureTillSept,
                request.ReasonForCorpusFund), ct);
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

    private async Task<AppendixCorpusFundDto> ToDtoAsync(AppendixCorpusFund e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixCorpusFund> { e }, ct);
        return dtos[0];
    }

    private async Task<List<AppendixCorpusFundDto>> ToDtosAsync(List<AppendixCorpusFund> entities, CancellationToken ct)
    {
        var abIds = entities.Select(e => e.AutonomousBodyId).Distinct().ToList();
        var abNames = await _db.AutonomousBodies.Where(a => abIds.Contains(a.AutonomousBodyId)).ToDictionaryAsync(a => a.AutonomousBodyId, a => a.Name, ct);

        return entities.Select(e => new AppendixCorpusFundDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            AutonomousBodyId = e.AutonomousBodyId,
            AutonomousBodyName = abNames.TryGetValue(e.AutonomousBodyId, out var name) ? name : null,
            IsPublicAccount = e.IsPublicAccount,
            AccumulatedBalancePrevYear = e.AccumulatedBalancePrevYear,
            AccumulatedBalance = e.AccumulatedBalance,
            ActualExpenditureY1 = e.ActualExpenditureY1,
            ActualExpenditureY2 = e.ActualExpenditureY2,
            ActualExpenditureY3 = e.ActualExpenditureY3,
            AllocationInBE = e.AllocationInBE,
            ExpenditureTillSept = e.ExpenditureTillSept,
            ReasonForCorpusFund = e.ReasonForCorpusFund,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
