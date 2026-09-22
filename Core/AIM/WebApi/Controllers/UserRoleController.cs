namespace UBIS.Services.Aim.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.Aim.Application.DTOs.User;
using UBIS.Services.Aim.Application.Interfaces;

/// <summary>
/// API controller for user role and permission operations.
/// Provides endpoints to retrieve user roles, permissions, and authorization data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserRoleController : ControllerBase
{
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<UserRoleController> _logger;

    public UserRoleController(
        IUserRoleService userRoleService,
        ILogger<UserRoleController> logger)
    {
        _userRoleService = userRoleService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves user role and permission information for a specific financial year.
    /// </summary>
    [HttpGet("my-role")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserRoleDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRole(
        [FromQuery] string financialYear,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized("User not authenticated.");
        }

        _logger.LogInformation("Role query for user: {UserId}, FY: {FinancialYear}", userId, financialYear);

        var request = new GetUserRoleRequestDto { FinancialYear = financialYear };
        var result = await _userRoleService.GetUserRoleAsync(userId.Value, request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return NotFound(result.Error);
    }

    /// <summary>
    /// Retrieves user role and permission for a specific user (admin endpoint).
    /// </summary>
    [HttpGet("{userId:int}/role")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserRoleDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRole(
        int userId,
        [FromQuery] string financialYear,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Role query for user: {UserId}, FY: {FinancialYear}", userId, financialYear);

        var request = new GetUserRoleRequestDto { FinancialYear = financialYear };
        var result = await _userRoleService.GetUserRoleAsync(userId, request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return NotFound(result.Error);
    }

    /// <summary>
    /// Retrieves all active roles in the system.
    /// </summary>
    [HttpGet("all-roles")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RoleDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllRoles(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All roles query");

        var result = await _userRoleService.GetAllRolesAsync(cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Retrieves role details by ID.
    /// </summary>
    [HttpGet("roles/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RoleDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Role details query for role: {RoleId}", roleId);

        var result = await _userRoleService.GetRoleByIdAsync(roleId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return NotFound(result.Error);
    }

    /// <summary>
    /// Retrieves all Functions a role has existence-based access to.
    /// </summary>
    [HttpGet("roles/{roleId:int}/functions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FunctionPermissionDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleFunctions(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Function permissions query for role: {RoleId}", roleId);

        var result = await _userRoleService.GetRoleFunctionsAsync(roleId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return NotFound(result.Error);
    }

    /// <summary>
    /// Checks if the caller has existence-based access to a Function for a financial year.
    /// </summary>
    [HttpGet("check-permission")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckPermission(
        [FromQuery] int functionId,
        [FromQuery] string financialYear,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized("User not authenticated.");
        }

        _logger.LogInformation("Permission check: User {UserId}, Function {FunctionId}", userId, functionId);

        var hasPermission = await _userRoleService.HasPermissionAsync(
            userId.Value, functionId, financialYear, cancellationToken);

        return Ok(new { HasPermission = hasPermission, UserId = userId, FunctionId = functionId });
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
