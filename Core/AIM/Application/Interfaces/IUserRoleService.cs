namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.DTOs.User;

/// <summary>
/// Service interface for user role and permission operations.
/// Provides methods to retrieve user roles, permissions, and authorization data.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Retrieves user role and permission information for a specific financial year.
    /// </summary>
    Task<Result<UserRoleDto>> GetUserRoleAsync(
        int userId,
        GetUserRoleRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active roles in the system.
    /// </summary>
    Task<Result<List<RoleDetailDto>>> GetAllRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves specific role by ID with all function mappings.
    /// </summary>
    Task<Result<RoleDetailDto>> GetRoleByIdAsync(
        int roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all Functions a role has existence-based access to.
    /// </summary>
    Task<Result<List<FunctionPermissionDto>>> GetRoleFunctionsAsync(
        int roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user (for a given financial year) has existence-based access to a Function.
    /// No CRUD granularity — see <see cref="FunctionPermissionDto"/> for why.
    /// </summary>
    Task<bool> HasPermissionAsync(
        int userId,
        int functionId,
        string financialYear,
        CancellationToken cancellationToken = default);
}
