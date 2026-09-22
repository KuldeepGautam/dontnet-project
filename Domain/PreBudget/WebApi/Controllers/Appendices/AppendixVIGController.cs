namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-G: User Charges of Autonomous Bodies / various organizations. New
/// appendix, 2026-09-09 - see UBIS-Pre-budget-html/Appendix6g.html.</summary>
[ApiController]
[Route("api/appendix-vig")]
[Authorize]
public class AppendixVIGController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixUserChargesAutonomousBody> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIGController(AppendixDataRepository<AppendixUserChargesAutonomousBody> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-09-09: "Unique entry from Demand in a Financial year per
        // Autonomous Body" - same uniqueness convention as VI-C/VI-E's own AutonomousBodyId check.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: null, ct))
        {
            var abName = await _db.AutonomousBodies.Where(a => a.AutonomousBodyId == request.AutonomousBodyId).Select(a => a.Name).FirstOrDefaultAsync(ct);
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Autonomous Body '{abName ?? request.AutonomousBodyId.ToString()}' already exists for this Demand." });
        }

        var entity = new AppendixUserChargesAutonomousBody { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.AutonomousBodyId,
            request.BriefOnRevenueSources,
            request.PresentStatus,
            request.ReceiptsCollected,
            request.TotalRevenueExpenditure,
            request.TotalCapitalExpenditure);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct)
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
                request.BriefOnRevenueSources,
                request.PresentStatus,
                request.ReceiptsCollected,
                request.TotalRevenueExpenditure,
                request.TotalCapitalExpenditure), ct);
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

    private async Task<AppendixUserChargesAutonomousBodyDto> ToDtoAsync(AppendixUserChargesAutonomousBody e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixUserChargesAutonomousBody> { e }, ct);
        return dtos[0];
    }

    private async Task<List<AppendixUserChargesAutonomousBodyDto>> ToDtosAsync(List<AppendixUserChargesAutonomousBody> entities, CancellationToken ct)
    {
        var abIds = entities.Select(e => e.AutonomousBodyId).Distinct().ToList();
        var abNames = await _db.AutonomousBodies.Where(a => abIds.Contains(a.AutonomousBodyId)).ToDictionaryAsync(a => a.AutonomousBodyId, a => a.Name, ct);

        return entities.Select(e => new AppendixUserChargesAutonomousBodyDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            AutonomousBodyId = e.AutonomousBodyId,
            AutonomousBodyName = abNames.TryGetValue(e.AutonomousBodyId, out var name) ? name : null,
            BriefOnRevenueSources = e.BriefOnRevenueSources,
            PresentStatus = e.PresentStatus,
            ReceiptsCollected = e.ReceiptsCollected,
            TotalRevenueExpenditure = e.TotalRevenueExpenditure,
            TotalCapitalExpenditure = e.TotalCapitalExpenditure,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
