namespace UBIS.Services.ReferenceData.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.ReferenceData.Application.DTOs;
using UBIS.Services.ReferenceData.Application.Interfaces;

/// <summary>Used by PreBudget's Appendix V-B screen.</summary>
[ApiController]
[Route("api/object-heads")]
[Authorize]
public class ObjectHeadsController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ObjectHeadsController(IReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetObjectHeads(CancellationToken ct)
    {
        var objectHeads = await _referenceDataService.GetObjectHeadsAsync(ct);
        return Ok(objectHeads);
    }

    [HttpPost]
    public async Task<IActionResult> CreateObjectHead([FromBody] CreateObjectHeadRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var objectHead = await _referenceDataService.CreateObjectHeadAsync(request, userId.Value, ct);
        return Ok(objectHead);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
