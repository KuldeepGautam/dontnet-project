namespace UBIS.Services.ReferenceData.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.ReferenceData.Application.DTOs;
using UBIS.Services.ReferenceData.Application.Interfaces;

[ApiController]
[Route("api/schemes")]
[Authorize]
public class SchemesController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public SchemesController(IReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    /// <summary>Used by PreBudget's Appendix III/IV/IV-A/IV-B/VI-B screens to populate the "Select Scheme" dropdown for a Demand.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSchemes([FromQuery] int? demandId, CancellationToken ct)
    {
        var schemes = await _referenceDataService.GetSchemesAsync(demandId, ct);
        return Ok(schemes);
    }

    [HttpGet("{schemeId:int}")]
    public async Task<IActionResult> GetScheme(int schemeId, CancellationToken ct)
    {
        var scheme = await _referenceDataService.GetSchemeAsync(schemeId, ct);
        return scheme is null
            ? NotFound(new { Code = "NOT_FOUND", Message = $"Scheme {schemeId} not found." })
            : Ok(scheme);
    }

    [HttpPost]
    public async Task<IActionResult> CreateScheme([FromBody] CreateSchemeRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var scheme = await _referenceDataService.CreateSchemeAsync(request, userId.Value, ct);
        return CreatedAtAction(nameof(GetScheme), new { schemeId = scheme.SchemeId }, scheme);
    }

    [HttpGet("sub-schemes")]
    public async Task<IActionResult> GetSubSchemes([FromQuery] int? schemeId, CancellationToken ct)
    {
        var subSchemes = await _referenceDataService.GetSubSchemesAsync(schemeId, ct);
        return Ok(subSchemes);
    }

    [HttpPost("sub-schemes")]
    public async Task<IActionResult> CreateSubScheme([FromBody] CreateSubSchemeRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var subScheme = await _referenceDataService.CreateSubSchemeAsync(request, userId.Value, ct);
        return Ok(subScheme);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
