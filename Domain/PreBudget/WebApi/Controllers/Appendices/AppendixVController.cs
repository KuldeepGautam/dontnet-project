namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix V: Estimates of Establishment &amp; Other Central Expenditure (own entry table, independent of V-A/B/C).</summary>
[ApiController]
[Route("api/appendix-v")]
[Authorize]
public class AppendixVController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixEstablishmentExpenditure> _repository;

    public AppendixVController(AppendixDataRepository<AppendixEstablishmentExpenditure> repository)
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
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixEstablishmentExpenditureDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var entity = new AppendixEstablishmentExpenditure { DemandId = request.DemandId, FinancialYear = request.FinancialYear };

        try
        {
            entity.UpdateFrom(
                request.Category,
                request.Actuals,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedRE,
                request.BudgetRecommendedRE,
                request.ProposedNBE,
                request.BudgetRecommendedNBE,
                request.RemarksBudget);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
        }

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixEstablishmentExpenditureDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.Category,
                request.Actuals,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedRE,
                request.BudgetRecommendedRE,
                request.ProposedNBE,
                request.BudgetRecommendedNBE,
                request.RemarksBudget), ct);
            return Ok(ToDto(updated));
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
            return Ok(ToDto(frozen));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    private static AppendixEstablishmentExpenditureDto ToDto(AppendixEstablishmentExpenditure e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        Category = e.Category,
        Actuals = e.Actuals,
        ActualsUptoSeptPrevYear = e.ActualsUptoSeptPrevYear,
        BE = e.BE,
        ActualsUptoSept = e.ActualsUptoSept,
        ProposedRE = e.ProposedRE,
        BudgetRecommendedRE = e.BudgetRecommendedRE,
        ProposedNBE = e.ProposedNBE,
        BudgetRecommendedNBE = e.BudgetRecommendedNBE,
        RemarksBudget = e.RemarksBudget,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
