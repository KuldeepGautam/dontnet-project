namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-D: Available internal resources with Grantee Bodies/Autonomous Institutions.</summary>
[ApiController]
[Route("api/appendix-vid")]
[Authorize]
public class AppendixVIDController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixInternalResources> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIDController(AppendixDataRepository<AppendixInternalResources> repository, PreBudgetDbContext db)
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
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixInternalResourcesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-08-27: "duplicate entry should not be allowed for Name of
        // GranteeBody/Autonomous Institution based on demand."
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Grantee Body/Autonomous Institution already exists for this Demand." });
        }

        var zeroOrBlankError = ValidateNonZeroAmounts(request);
        if (zeroOrBlankError != null)
        {
            return BadRequest(zeroOrBlankError);
        }

        var entity = new AppendixInternalResources { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.AutonomousBodyId,
            request.AsOnMarch31,
            request.AsOnJune30,
            request.ExpectedNextMarch31,
            request.ExpectedNextFY,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixInternalResourcesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.AutonomousBodyId == request.AutonomousBodyId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Grantee Body/Autonomous Institution already exists for this Demand." });
        }

        var zeroOrBlankError = ValidateNonZeroAmounts(request);
        if (zeroOrBlankError != null)
        {
            return BadRequest(zeroOrBlankError);
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.AutonomousBodyId,
                request.AsOnMarch31,
                request.AsOnJune30,
                request.ExpectedNextMarch31,
                request.ExpectedNextFY,
                request.Remarks), ct);
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

    /// <summary>
    /// Client requirement 2026-08-31: "0 or blank values cannot be entered" - server-side is the
    /// real gate per this repo's convention (the client-side check in pre-budget-meeting.js's
    /// Submit handler is a convenience). Also enforces "remarks textbox ... maximum length should
    /// be 1000" here, since a client-side maxlength attribute alone can be bypassed.
    /// </summary>
    private static object? ValidateNonZeroAmounts(SaveAppendixInternalResourcesDto request)
    {
        if (request.AsOnMarch31 is null or 0 || request.AsOnJune30 is null or 0
            || request.ExpectedNextMarch31 is null or 0 || request.ExpectedNextFY is null or 0)
        {
            return new { Code = "VALIDATION_FAILED", Message = "Enter non-zero values for all amount fields." };
        }

        if ((request.Remarks?.Length ?? 0) > 1000)
        {
            return new { Code = "VALIDATION_FAILED", Message = "Remarks cannot exceed 1000 characters." };
        }

        return null;
    }

    private async Task<AppendixInternalResourcesDto> ToDtoAsync(AppendixInternalResources e, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixInternalResources> { e }, ct);
        return dtos[0];
    }

    /// <summary>Batches the Autonomous Body name lookup into 1 query, same pattern as Appendix VII-A's Major Head resolution.</summary>
    private async Task<List<AppendixInternalResourcesDto>> ToDtosAsync(List<AppendixInternalResources> entities, CancellationToken ct)
    {
        var autonomousBodyIds = entities.Where(e => e.AutonomousBodyId.HasValue).Select(e => e.AutonomousBodyId!.Value).Distinct().ToList();
        var autonomousBodyNames = await _db.AutonomousBodies
            .Where(a => autonomousBodyIds.Contains(a.AutonomousBodyId))
            .ToDictionaryAsync(a => a.AutonomousBodyId, a => a.Name, ct);

        return entities.Select(e => new AppendixInternalResourcesDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            AutonomousBodyId = e.AutonomousBodyId,
            // "Name of Grantee body is not saving or coming on grid" (client testing feedback,
            // 2026-08-25) - rows saved before NameOfInstitute switched back to an AutonomousBodyId
            // dropdown (2026-08-24) only have NameOfInstitute set, no AutonomousBodyId; without this
            // fallback the grid showed "-" for every one of them.
            AutonomousBodyName = e.AutonomousBodyId.HasValue && autonomousBodyNames.TryGetValue(e.AutonomousBodyId.Value, out var n) ? n : e.NameOfInstitute,
            AsOnMarch31 = e.AsOnMarch31,
            AsOnJune30 = e.AsOnJune30,
            ExpectedNextMarch31 = e.ExpectedNextMarch31,
            ExpectedNextFY = e.ExpectedNextFY,
            Remarks = e.Remarks,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
