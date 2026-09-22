namespace UBIS.Services.ReferenceData.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.ReferenceData.Application.DTOs;
using UBIS.Services.ReferenceData.Application.Interfaces;

/// <summary>Used by PreBudget's Appendix VII-A/VII-B/Public-Account-Template screens.</summary>
[ApiController]
[Route("api/major-heads")]
[Authorize]
public class MajorHeadsController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public MajorHeadsController(IReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMajorHeads(CancellationToken ct)
    {
        var majorHeads = await _referenceDataService.GetMajorHeadsAsync(ct);
        return Ok(majorHeads);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMajorHead([FromBody] CreateMajorHeadRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var majorHead = await _referenceDataService.CreateMajorHeadAsync(request, userId.Value, ct);
        return Ok(majorHead);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
