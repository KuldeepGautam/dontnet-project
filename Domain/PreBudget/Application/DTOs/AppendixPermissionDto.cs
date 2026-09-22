namespace UBIS.Services.PreBudget.Application.DTOs;

/// <summary>
/// Appendix-level, role-based permission flags read from dbo.M_MapRoleAppendix (renamed 2026-08-04
/// from M_RoleAppendixMapping). Fails closed:
/// a role/appendix pair with no mapping row returns all flags false (see
/// AppendixPermissionService.GetPermissionsAsync), not "everything allowed".
/// </summary>
public class AppendixPermissionDto
{
    public string AppendixCode { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanApprove { get; set; }

    public static AppendixPermissionDto NoAccess(string appendixCode) => new() { AppendixCode = appendixCode };
}
