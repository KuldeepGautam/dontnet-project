namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix V-A: Grant in Aid to Autonomous and Other Bodies.</summary>
[ApiController]
[Route("api/appendix-va")]
[Authorize]
public class AppendixVAController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixGrantInAid> _repository;

    public AppendixVAController(AppendixDataRepository<AppendixGrantInAid> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixGrantInAidDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "there should be single entry in Name of Autonomous Body
        // with respect to demand" - same uniqueness convention as Appendix VI-C.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Autonomous Body already exists for this Demand." });
        }

        var entity = new AppendixGrantInAid { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.AutonomousBodyId,
            request.GiaGeneralActuals,
            request.GiaGeneralActualsUptoSeptPrevYear,
            request.GiaGeneralBE,
            request.GiaGeneralActualsUptoSept,
            request.GiaGeneralRE,
            request.GiaGeneralNBE,
            request.GiaCcaActuals,
            request.GiaCcaActualsUptoSeptPrevYear,
            request.GiaCcaBE,
            request.GiaCcaActualsUptoSept,
            request.GiaCcaRE,
            request.GiaCcaNBE,
            request.GiaSalaryActuals,
            request.GiaSalaryActualsUptoSeptPrevYear,
            request.GiaSalaryTotal,
            request.GiaSalaryBE,
            request.GiaSalaryActualsUptoSept,
            request.GiaSalaryRE,
            request.GiaSalaryNBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixGrantInAidDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Autonomous Body already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.AutonomousBodyId,
                request.GiaGeneralActuals,
                request.GiaGeneralActualsUptoSeptPrevYear,
                request.GiaGeneralBE,
                request.GiaGeneralActualsUptoSept,
                request.GiaGeneralRE,
                request.GiaGeneralNBE,
                request.GiaCcaActuals,
                request.GiaCcaActualsUptoSeptPrevYear,
                request.GiaCcaBE,
                request.GiaCcaActualsUptoSept,
                request.GiaCcaRE,
                request.GiaCcaNBE,
                request.GiaSalaryActuals,
                request.GiaSalaryActualsUptoSeptPrevYear,
                request.GiaSalaryTotal,
                request.GiaSalaryBE,
                request.GiaSalaryActualsUptoSept,
                request.GiaSalaryRE,
                request.GiaSalaryNBE), ct);
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

    private static AppendixGrantInAidDto ToDto(AppendixGrantInAid e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        AutonomousBodyId = e.AutonomousBodyId,
        GiaGeneralActuals = e.GiaGeneralActuals,
        GiaGeneralActualsUptoSeptPrevYear = e.GiaGeneralActualsUptoSeptPrevYear,
        GiaGeneralBE = e.GiaGeneralBE,
        GiaGeneralActualsUptoSept = e.GiaGeneralActualsUptoSept,
        GiaGeneralRE = e.GiaGeneralRE,
        GiaGeneralNBE = e.GiaGeneralNBE,
        GiaCcaActuals = e.GiaCcaActuals,
        GiaCcaActualsUptoSeptPrevYear = e.GiaCcaActualsUptoSeptPrevYear,
        GiaCcaBE = e.GiaCcaBE,
        GiaCcaActualsUptoSept = e.GiaCcaActualsUptoSept,
        GiaCcaRE = e.GiaCcaRE,
        GiaCcaNBE = e.GiaCcaNBE,
        GiaSalaryActuals = e.GiaSalaryActuals,
        GiaSalaryActualsUptoSeptPrevYear = e.GiaSalaryActualsUptoSeptPrevYear,
        GiaSalaryTotal = e.GiaSalaryTotal,
        GiaSalaryBE = e.GiaSalaryBE,
        GiaSalaryActualsUptoSept = e.GiaSalaryActualsUptoSept,
        GiaSalaryRE = e.GiaSalaryRE,
        GiaSalaryNBE = e.GiaSalaryNBE,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
