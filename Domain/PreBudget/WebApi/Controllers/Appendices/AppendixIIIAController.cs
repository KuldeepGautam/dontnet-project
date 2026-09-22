namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Appendix III-A: TSA Assignment and Expenditure — the screenshot-confirmed reference appendix
/// (evaluation doc §5). Enforces FRS BR-04/BR-05 (Actual Expenditure ≤ TSA Assignment ≤ BE) and
/// auto-calculates Unspent Assignment, both confirmed missing from the designer prototype's
/// client-side-only form (evaluation doc §11).
/// </summary>
[ApiController]
[Route("api/appendix-iiia")]
[Authorize]
public class AppendixIIIAController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixTsaAssignment> _repository;

    public AppendixIIIAController(AppendixDataRepository<AppendixTsaAssignment> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        // Client requirement 2026-08-27: "grid order by entry date and entity name."
        var ordered = records
            .OrderBy(e => e.CreatedOnDate)
            .ThenBy(e => e.EntityName, StringComparer.OrdinalIgnoreCase);
        return Ok(ordered.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixTsaAssignmentDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        // Client requirement 2026-08-27: "demand and Name of Entity record cannot have multiple
        // entries." Case-insensitive - two rows differing only by EntityName's casing are still the
        // same entity for this purpose.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => string.Equals(e.EntityName, request.EntityName, StringComparison.OrdinalIgnoreCase), excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "The Entity Name already exists for this demand." });
        }

        var entity = new AppendixTsaAssignment
        {
            DemandId = request.DemandId,
            FinancialYear = request.FinancialYear
        };

        try
        {
            entity.UpdateFrom(
                request.EntityName,
                request.BE,
                request.TsaAssignmentAsOnSept,
                request.ActualExpenditureUptoSept,
                request.DateOfLastAssignment,
                request.AmountOfLastAssignment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
        }

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixTsaAssignmentDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => string.Equals(e.EntityName, request.EntityName, StringComparison.OrdinalIgnoreCase), excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "The Entity Name already exists for this demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.EntityName,
                request.BE,
                request.TsaAssignmentAsOnSept,
                request.ActualExpenditureUptoSept,
                request.DateOfLastAssignment,
                request.AmountOfLastAssignment), ct);
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

    private static AppendixTsaAssignmentDto ToDto(AppendixTsaAssignment e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        EntityName = e.EntityName,
        BE = e.BE,
        TsaAssignmentAsOnSept = e.TsaAssignmentAsOnSept,
        ActualExpenditureUptoSept = e.ActualExpenditureUptoSept,
        UnspentAssignment = e.UnspentAssignment,
        DateOfLastAssignment = e.DateOfLastAssignment,
        AmountOfLastAssignment = e.AmountOfLastAssignment,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
