namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>
/// Response DTO for user role and permission information.
/// </summary>
public class UserRoleDto
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Assigned role for the financial year.</summary>
    public RoleDetailDto Role { get; set; } = new();

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Functions this role has existence-based access to (added 2026-07-13).</summary>
    public List<FunctionPermissionDto> FunctionPermissions { get; set; } = new();
}

/// <summary>
/// Response DTO for role details.
/// </summary>
public class RoleDetailDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for a Function this Role has existence-based access to. No CRUD granularity —
/// the real <c>dbo.M_MapRoleFunction</c> table has no such columns (see AuthenticationService's
/// doc comments for the full rationale). Added 2026-07-13, replaces the earlier invented
/// CanCreate/CanRead/CanUpdate/CanDelete/PermissionMatrix shape.
/// </summary>
public class FunctionPermissionDto
{
    public int FunctionId { get; set; }

    public string FunctionName { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO to query user role and permissions.
/// </summary>
public class GetUserRoleRequestDto
{
    /// <summary>
    /// Financial year for scoped role retrieval.
    /// </summary>
    public string FinancialYear { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for logout operation.
/// </summary>
public class LogoutResultDto
{
    /// <summary>
    /// Indicates if logout was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of logout (UTC).
    /// </summary>
    public DateTime LogoutAt { get; set; }
}
