namespace UBIS.Services.PreBudget.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>FR-003: RE Data Remarks — Budget Division and ABO/DS/Director users only, see RemarkRoleOptions.</summary>
[ApiController]
[Route("api/remarks")]
[Authorize]
public class RemarksController : ControllerBase
{
    private readonly IRemarkService _remarkService;

    public RemarksController(IRemarkService remarkService)
    {
        _remarkService = remarkService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRemarks([FromQuery] int demandId, [FromQuery] int appendixId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var roleName = GetRoleNameFromClaims();
        if (roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no role claim." });
        }

        try
        {
            var remarks = await _remarkService.GetRemarksAsync(demandId, appendixId, financialYear, roleName, ct);
            return Ok(remarks);
        }
        catch (RemarkAccessDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRemark([FromBody] CreateRemarkDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var roleName = GetRoleNameFromClaims();
        if (userId is null || roleName is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token missing user id or role claim." });
        }

        try
        {
            var remark = await _remarkService.CreateRemarkAsync(request, userId.Value, roleName, ct);
            return Ok(remark);
        }
        catch (RemarkAccessDeniedException ex)
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
