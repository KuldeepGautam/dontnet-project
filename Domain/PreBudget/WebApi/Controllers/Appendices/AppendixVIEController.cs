namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-E: Autonomous Bodies for which Corpus Fund has been created out of GiA support.</summary>
[ApiController]
[Route("api/appendix-vie")]
[Authorize]
public class AppendixVIEController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixCorpusFundAbGia> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIEController(AppendixDataRepository<AppendixCorpusFundAbGia> repository, PreBudgetDbContext db)
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
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-09-09: "single entry per demand per Autonomous Body" - same
        // uniqueness rule/message shape as AppendixVICController (VI-C) for the same reason
        // (2026-08-27: "uniqueness for demand - autonomous body").
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: null, ct))
        {
            var abName = await _db.AutonomousBodies.Where(a => a.AutonomousBodyId == request.AutonomousBodyId).Select(a => a.Name).FirstOrDefaultAsync(ct);
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Autonomous Body '{abName ?? request.AutonomousBodyId.ToString()}' already exists for this Demand." });
        }

        var entity = new AppendixCorpusFundAbGia { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.AutonomousBodyId,
            request.CorpusFundBalance1,
            request.CorpusFundBalance2,
            request.CorpusFundBankName,
            request.CorpusFundReason,
            request.GiaRE,
            request.GiaBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct)
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
                request.CorpusFundBalance1,
                request.CorpusFundBalance2,
                request.CorpusFundBankName,
                request.CorpusFundReason,
                request.GiaRE,
                request.GiaBE), ct);
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

    private async Task<AppendixCorpusFundAbGiaDto> ToDtoAsync(AppendixCorpusFundAbGia e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixCorpusFundAbGia> { e }, ct);
        return dtos[0];
    }

    private async Task<List<AppendixCorpusFundAbGiaDto>> ToDtosAsync(List<AppendixCorpusFundAbGia> entities, CancellationToken ct)
    {
        var abIds = entities.Select(e => e.AutonomousBodyId).Distinct().ToList();
        var abNames = await _db.AutonomousBodies.Where(a => abIds.Contains(a.AutonomousBodyId)).ToDictionaryAsync(a => a.AutonomousBodyId, a => a.Name, ct);

        return entities.Select(e => new AppendixCorpusFundAbGiaDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            AutonomousBodyId = e.AutonomousBodyId,
            AutonomousBodyName = abNames.TryGetValue(e.AutonomousBodyId, out var name) ? name : null,
            CorpusFundBalance1 = e.CorpusFundBalance1,
            CorpusFundBalance2 = e.CorpusFundBalance2,
            CorpusFundBankName = e.CorpusFundBankName,
            CorpusFundReason = e.CorpusFundReason,
            GiaRE = e.GiaRE,
            GiaBE = e.GiaBE,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
