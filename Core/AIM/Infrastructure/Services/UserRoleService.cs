namespace UBIS.Services.Aim.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.DTOs.User;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Persistence;

/// <summary>
/// Implements user role and permission retrieval service against the real, DBA-owned
/// <c>dbo.M_Users</c>/<c>M_MapUserRole</c>/<c>M_Role</c>/<c>M_MapRoleFunction</c> tables.
/// Rewritten 2026-07-13 — Role is resolved via <c>M_MapUserRole</c> (reintroduced, user-confirmed;
/// the legacy <c>User.Role</c> column is no longer authoritative, renamed 2026-08-04 from
/// M_MapUserDemandFY/consolidated to one row per user) and there's no CRUD permission matrix
/// (existence-based Function access only).
/// </summary>
public class UserRoleService : IUserRoleService
{
    private readonly AimDbContext _dbContext;

    public UserRoleService(AimDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserRoleDto>> GetUserRoleAsync(
        int userId,
        GetUserRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                return Result<UserRoleDto>.Failure(Error.UserNotFound(userId.ToString()));
            }

            // M_User is one row per person (as of the 2026-08-03 consolidation) — eligibility for
            // the requested financial year is now checked against dbo.M_MapUserFY instead of a
            // combined UserId+FinancialYear lookup on M_User itself.
            var eligibleForYear = await _dbContext.UserFinancialYears
                .AnyAsync(fy => fy.UserId == userId && fy.FinancialYear == request.FinancialYear, cancellationToken);

            if (!eligibleForYear)
            {
                return Result<UserRoleDto>.Failure(Error.UserNotFound(userId.ToString()));
            }

            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);

            if (userCharge == null)
            {
                return Result<UserRoleDto>.Failure(Error.NotFound("MapUserRole", userId.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == userCharge.RoleId, cancellationToken);

            if (role == null)
            {
                return Result<UserRoleDto>.Failure(Error.NotFound("Role", $"{userId}:{userCharge.RoleId}"));
            }

            var functionPermissions = await _dbContext.RoleFunctionMappings
                .Include(rfm => rfm.Function)
                .Where(rfm => rfm.RoleId == role.RoleId && rfm.Active != "N" && rfm.RFFreez != "Y")
                .Select(rfm => new FunctionPermissionDto
                {
                    FunctionId = rfm.FunctionId,
                    FunctionName = rfm.Function.FunctionName
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            return Result<UserRoleDto>.Success(new UserRoleDto
            {
                UserId = user.UserId,
                UserName = user.Username,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = new RoleDetailDto { RoleId = role.RoleId, RoleName = role.RoleName },
                FinancialYear = request.FinancialYear,
                FunctionPermissions = functionPermissions
            });
        }
        catch (Exception ex)
        {
            return Result<UserRoleDto>.Failure(Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<List<RoleDetailDto>>> GetAllRolesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _dbContext.Roles
                .Where(r => r.Active != "N")
                .Select(r => new RoleDetailDto { RoleId = r.RoleId, RoleName = r.RoleName })
                .ToListAsync(cancellationToken);

            return Result<List<RoleDetailDto>>.Success(roles);
        }
        catch (Exception ex)
        {
            return Result<List<RoleDetailDto>>.Failure(Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<RoleDetailDto>> GetRoleByIdAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
            if (role == null)
            {
                return Result<RoleDetailDto>.Failure(Error.NotFound("Role", roleId));
            }

            return Result<RoleDetailDto>.Success(new RoleDetailDto { RoleId = role.RoleId, RoleName = role.RoleName });
        }
        catch (Exception ex)
        {
            return Result<RoleDetailDto>.Failure(Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<List<FunctionPermissionDto>>> GetRoleFunctionsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _dbContext.RoleFunctionMappings
                .Include(rfm => rfm.Function)
                .Where(rfm => rfm.RoleId == roleId && rfm.Active != "N" && rfm.RFFreez != "Y")
                .Select(rfm => new FunctionPermissionDto
                {
                    FunctionId = rfm.FunctionId,
                    FunctionName = rfm.Function.FunctionName
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            return Result<List<FunctionPermissionDto>>.Success(permissions);
        }
        catch (Exception ex)
        {
            return Result<List<FunctionPermissionDto>>.Failure(Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<bool> HasPermissionAsync(
        int userId,
        int functionId,
        string financialYear,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
                return false;

            var eligibleForYear = await _dbContext.UserFinancialYears
                .AnyAsync(fy => fy.UserId == userId && fy.FinancialYear == financialYear, cancellationToken);

            if (!eligibleForYear)
                return false;

            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);

            if (userCharge == null)
                return false;

            return await _dbContext.RoleFunctionMappings
                .AnyAsync(
                    rfm => rfm.RoleId == userCharge.RoleId && rfm.FunctionId == functionId
                        && rfm.Active != "N" && rfm.RFFreez != "Y",
                    cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
