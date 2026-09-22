namespace UBIS.Services.Ecl.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.Ecl.Application.DTOs;
using UBIS.Services.Ecl.Application.Interfaces;
using UBIS.Services.Ecl.Infrastructure.Security;

/// <summary>
/// DOE-only approve/reject/reapproval-grant actions. [ServiceFilter(typeof(RequireDoeRoleFilter))]
/// enforces the role gate server-side on every action here — a Demand-role user holding a valid JWT
/// must not be able to bypass this by calling the API directly (the actual reason this check lives
/// here rather than only in UBIS_Web's UI).
/// </summary>
[ApiController]
[Route("api/ecl/approval")]
[Authorize]
[ServiceFilter(typeof(RequireDoeRoleFilter))]
public class EclApprovalController : ControllerBase
{
    private readonly IEclOutlayRepository _repository;

    public EclApprovalController(IEclOutlayRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("{rowId:int}/approve")]
    public async Task<IActionResult> Approve(int rowId, [FromBody] DoeApprovalActionRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.ApproveAsync(rowId, userId.Value, request.DoeRemarks, ct);

        return result.IsSuccess ? Ok(EclOutlayController.ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    [HttpPost("{rowId:int}/reject")]
    public async Task<IActionResult> Reject(int rowId, [FromBody] DoeApprovalActionRequestDto request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var result = await _repository.RejectAsync(rowId, userId.Value, request.DoeRemarks, ct);

        return result.IsSuccess ? Ok(EclOutlayController.ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    [HttpPost("{rowId:int}/request-reapproval")]
    public async Task<IActionResult> RequestReapproval(int rowId, CancellationToken ct)
    {
        var result = await _repository.RequestReapprovalAsync(rowId, ct);

        return result.IsSuccess ? Ok(EclOutlayController.ToDto(result.Data!)) : ToActionResult(result.Error!);
    }

    private static IActionResult ToActionResult(Error error) => new ObjectResult(error)
    {
        StatusCode = error.Code switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        }
    };
}
