namespace UBIS.Services.UserProfile.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.UserProfile.Application.DTOs;
using UBIS.Services.UserProfile.Application.Interfaces;

[ApiController]
[Route("api/userprofile")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IAimProfileClient _aimProfileClient;
    private readonly IUserIpRequestService _ipRequestService;

    public UserProfileController(IAimProfileClient aimProfileClient, IUserIpRequestService ipRequestService)
    {
        _aimProfileClient = aimProfileClient;
        _ipRequestService = ipRequestService;
    }

    /// <summary>
    /// Everything the UserProfile page needs in one call: UserName/RoleName/LastLoginDate +
    /// AllowedIpAddressOne/Two (both from AIM, this service's own DB has none of that), plus this
    /// user's latest IP change-request status (from this service's own dbo.M_UserIPrequest read).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var bearerToken = GetBearerToken();
        if (bearerToken is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Missing bearer token." });
        }

        var snapshot = await _aimProfileClient.GetProfileSnapshotAsync(bearerToken, ct);
        var latestRequest = await _ipRequestService.GetLatestRequestAsync(userId.Value, ct);

        return Ok(new UserProfileSummaryDto
        {
            UserName = snapshot?.UserName,
            RoleName = snapshot?.RoleName,
            LastLoginDate = snapshot?.LastLoginDate,
            AllowedIpAddressOne = snapshot?.AllowedIpAddressOne,
            AllowedIpAddressTwo = snapshot?.AllowedIpAddressTwo,
            Email = snapshot?.Email,
            MaskedMobile = snapshot?.MaskedMobile,
            LatestRequest = latestRequest
        });
    }

    /// <summary>History grid for the merged User Profile page — all requests, not just the latest.</summary>
    [HttpGet("ip-requests")]
    public async Task<IActionResult> GetMyIpRequests(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        var requests = await _ipRequestService.GetAllRequestsAsync(userId.Value, ct);
        return Ok(requests);
    }

    /// <summary>
    /// Raises a new pending IP change request. ExistingIp is trusted as supplied by the caller
    /// (UBIS_Web) — it captured the real client IP from its own inbound request; this service
    /// cannot determine that itself since UBIS_Web is its actual TCP caller.
    /// </summary>
    [HttpPost("ip-change-request")]
    public async Task<IActionResult> RaiseIpChangeRequest([FromBody] RaiseIpChangeRequestDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        if (string.IsNullOrWhiteSpace(request.IPAddress1) && string.IsNullOrWhiteSpace(request.IPAddress2))
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = "At least one of IPAddress1/IPAddress2 is required." });
        }

        var result = await _ipRequestService.RaiseIpChangeRequestAsync(userId.Value, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Direct Email/Mobile update — forwards to AIM's PUT /api/users/contact (AIM stays the sole
    /// M_User writer). Saves immediately, unlike the IP change-request flow above.
    /// </summary>
    [HttpPut("contact")]
    public async Task<IActionResult> UpdateContact([FromBody] UpdateContactRequestDto request, CancellationToken ct)
    {
        var bearerToken = GetBearerToken();
        if (bearerToken is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Missing bearer token." });
        }

        var success = await _aimProfileClient.UpdateContactAsync(bearerToken, request.Email, request.Mobile, ct);
        if (!success)
        {
            return BadRequest(new { Code = "UPDATE_FAILED", Message = "Could not update contact details." });
        }

        return Ok(new { request.Email, request.Mobile });
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..]
            : null;
    }
}
