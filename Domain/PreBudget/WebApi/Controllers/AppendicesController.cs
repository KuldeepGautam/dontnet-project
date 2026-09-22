namespace UBIS.Services.PreBudget.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>Select-Demand-and-Appendix screen (replaces old sp_Select_Demands_Template) + FRS Dashboard "Status" widget.</summary>
[ApiController]
[Route("api/appendices")]
[Authorize]
public class AppendicesController : ControllerBase
{
    private readonly IAppendixService _appendixService;

    public AppendicesController(IAppendixService appendixService)
    {
        _appendixService = appendixService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppendices([FromQuery] string financialYear, CancellationToken ct)
    {
        var appendices = await _appendixService.GetAppendicesAsync(financialYear, ct);
        return Ok(appendices);
    }

    /// <summary>Per-Demand status list — the caller (UBIS_Web) is responsible for having already verified the user may see this Demand via M_MapUserRole.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetAppendicesWithStatus([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var appendices = await _appendixService.GetAppendicesWithStatusAsync(demandId, financialYear, ct);
        return Ok(appendices);
    }

    [HttpPost("nil-submission")]
    public async Task<IActionResult> SetNilSubmission([FromBody] SetNilSubmissionDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        try
        {
            await _appendixService.SetNilSubmissionAsync(request, userId.Value, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "NIL_SUBMISSION_FAILED", Message = ex.Message });
        }
    }

    [HttpPost("{appendixId:int}/freeze")]
    public async Task<IActionResult> FreezeSubmission(int appendixId, [FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        await _appendixService.FreezeSubmissionAsync(demandId, appendixId, financialYear, userId.Value, ct);
        return Ok();
    }

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] AllocateAppendixDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var bearerToken = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        var result = await _appendixService.AllocateAsync(request, userId.Value, bearerToken, ct);
        return Ok(result);
    }

    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations([FromQuery] int? appendixId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var allocations = await _appendixService.GetAllocationsAsync(appendixId, financialYear, ct);
        return Ok(allocations);
    }

    [HttpPut("allocations")]
    public async Task<IActionResult> UpdateAllocation([FromBody] UpdateAllocationDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        await _appendixService.UpdateAllocationAsync(request, userId.Value, ct);
        return Ok();
    }

    /// <summary>
    /// AJAX-only: narrows a candidate DemandId list (already permission-checked by UBIS_Web/AIM's
    /// GetMyDemands) down to only Demands that actually have an Appendix Allocation for this
    /// FinancialYear - drives the Pre-Budget Data and Report page's Demand dropdown, which should
    /// only ever list Demands this workflow has been set up for. See
    /// IAppendixService.GetAllocatedDemandIdsAsync's own doc comment for the "3 entries per demand"
    /// bug this replaces (client report 2026-09-10).
    /// </summary>
    [HttpGet("allocated-demand-ids")]
    public async Task<IActionResult> GetAllocatedDemandIds([FromQuery] string demandIds, [FromQuery] string financialYear, CancellationToken ct)
    {
        var parsedIds = (demandIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var value) ? value : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var allocatedIds = await _appendixService.GetAllocatedDemandIdsAsync(parsedIds, financialYear, ct);
        return Ok(allocatedIds);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
