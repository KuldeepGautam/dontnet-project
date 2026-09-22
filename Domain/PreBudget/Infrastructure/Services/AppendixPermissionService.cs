namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Infrastructure.Persistence;

/// <summary>
/// Appendix-level permission lookup backed by dbo.M_MapRoleAppendix (renamed 2026-08-04 from
/// M_RoleAppendixMapping; see Others\publish-staging\create-role-appendix-permission-mapping.sql
/// for the original table). Introduced for VII-A, VII-B, XI and PA-ReceiptPayment - the legacy
/// system never had per-appendix permissions for these four screens (only a single coarse Active
/// flag on the shared "SelectAppendix.aspx" dispatcher function, confirmed against
/// BIMSDemo.dbo.M_RoleFunctionMapping/M_Function), so this table and service establish real
/// per-appendix, per-action granularity going forward.
///
/// dbo.M_MapRoleAppendix is a plain SQL table (DB-first, not owned by an EF entity/DbSet like
/// the appendix data tables), read here with Database.SqlQueryRaw joined against dbo.M_Role by
/// RoleName, since the JWT only carries the role's name (the "RoleName" claim), not its RoleId.
///
/// Fails closed: no row for the (role, appendix) pair - or no role name at all - returns every flag
/// false, never "allow by default".
/// </summary>
public class AppendixPermissionService
{
    private readonly PreBudgetDbContext _db;
    private readonly ILogger<AppendixPermissionService> _logger;

    public AppendixPermissionService(PreBudgetDbContext db, ILogger<AppendixPermissionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private record PermissionRow(bool CanView, bool CanCreate, bool CanEdit, bool CanDelete, bool CanSubmit, bool CanApprove);

    public async Task<AppendixPermissionDto> GetPermissionsAsync(string? roleName, string appendixCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(appendixCode))
        {
            _logger.LogWarning("Appendix permission check with missing role name or appendix code (role={RoleName}, appendix={AppendixCode}) - denying access.", roleName, appendixCode);
            return AppendixPermissionDto.NoAccess(appendixCode ?? string.Empty);
        }

        const string sql = """
            SELECT TOP 1 m.CanView, m.CanCreate, m.CanEdit, m.CanDelete, m.CanSubmit, m.CanApprove
            FROM dbo.M_MapRoleAppendix m
            INNER JOIN dbo.M_Role r ON r.RoleId = m.RoleId
            WHERE r.RoleName = {0} AND m.AppendixCode = {1}
            """;

        var row = await _db.Database.SqlQueryRaw<PermissionRow>(sql, roleName, appendixCode).FirstOrDefaultAsync(ct);

        if (row is null)
        {
            _logger.LogInformation("No M_MapRoleAppendix row for role '{RoleName}' / appendix '{AppendixCode}' - denying access (fail closed).", roleName, appendixCode);
            return AppendixPermissionDto.NoAccess(appendixCode);
        }

        return new AppendixPermissionDto
        {
            AppendixCode = appendixCode,
            CanView = row.CanView,
            CanCreate = row.CanCreate,
            CanEdit = row.CanEdit,
            CanDelete = row.CanDelete,
            CanSubmit = row.CanSubmit,
            CanApprove = row.CanApprove
        };
    }
}
