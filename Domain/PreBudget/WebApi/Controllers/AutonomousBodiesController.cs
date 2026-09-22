namespace UBIS.Services.PreBudget.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>FR-004: Autonomous Master for Appendix VI-C/VI-D/VI-E, plus the request/approval workflow.</summary>
[ApiController]
[Route("api/autonomous-bodies")]
[Authorize]
public class AutonomousBodiesController : ControllerBase
{
    private readonly IAutonomousBodyService _autonomousBodyService;

    public AutonomousBodiesController(IAutonomousBodyService autonomousBodyService)
    {
        _autonomousBodyService = autonomousBodyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAutonomousBodies([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var bodies = await _autonomousBodyService.GetAutonomousBodiesAsync(demandId, financialYear, ct);
        return Ok(bodies);
    }

    /// <summary>Appendix V-A's dropdown only, distinct by Name - now Demand- AND FinancialYear-scoped (client requirement, 2026-08-25), matching GetAutonomousBodies above.</summary>
    [HttpGet("for-appendix-va")]
    public async Task<IActionResult> GetAutonomousBodiesForAppendixVA([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var bodies = await _autonomousBodyService.GetAutonomousBodiesForAppendixVAAsync(demandId, financialYear, ct);
        return Ok(bodies);
    }

    /// <summary>Administrator-only direct add - client design 2026-08-10, matches the legacy "Add Autonomous/Grantee Name" form (Select Demand, Name English, Name Hindi).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAutonomousBody([FromBody] CreateAutonomousBodyDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var roleName = GetRoleNameFromClaims();
        if (userId is null || roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token missing user id or role claim." });
        }

        try
        {
            var result = await _autonomousBodyService.CreateAutonomousBodyAsync(request, userId.Value, roleName, ct);
            return Ok(result);
        }
        catch (AutonomousBodyAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    /// <summary>Ministry user's "my Autonomous Body isn't listed" request — surfaced in the VI-C/VI-D/VI-E screens per the designer prototype's helper text.</summary>
    [HttpPost("requests")]
    public async Task<IActionResult> RequestNewAutonomousBody([FromBody] CreateAutonomousBodyRequestDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _autonomousBodyService.RequestNewAutonomousBodyAsync(request, userId.Value, ct);
        return Ok(result);
    }

    /// <summary>Administrator's inbox of pending requests.</summary>
    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var roleName = GetRoleNameFromClaims();
        if (roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no role claim." });
        }

        try
        {
            var requests = await _autonomousBodyService.GetPendingRequestsAsync(roleName, ct);
            return Ok(requests);
        }
        catch (AutonomousBodyAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    [HttpPost("requests/{requestId:int}/review")]
    public async Task<IActionResult> ReviewRequest(int requestId, [FromBody] ReviewAutonomousBodyRequestDto review, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var roleName = GetRoleNameFromClaims();
        if (userId is null || roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token missing user id or role claim." });
        }

        try
        {
            var result = await _autonomousBodyService.ReviewRequestAsync(requestId, review, userId.Value, roleName, ct);
            return Ok(result);
        }
        catch (AutonomousBodyAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
