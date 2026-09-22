namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Infrastructure.Persistence;

/// <summary>
/// Generic CRUD + freeze operations shared by all 22 appendix data tables (every one inherits
/// AppendixEntityBase's Id/DemandId/FinancialYear/IsFrozen shape). Each appendix's WebApi
/// controller wraps this with its own entity-specific request/response DTOs — this class only
/// handles the shape common to every appendix, per the design doc §10's recommendation to avoid
/// hand-writing 22 near-identical CRUD implementations.
/// </summary>
public class AppendixDataRepository<TEntity> where TEntity : AppendixEntityBase, new()
{
    private readonly PreBudgetDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppendixDataRepository(PreBudgetDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Filters by DemandNo (stable across FY rollovers), not DemandId - same reasoning as
    /// AutonomousBodyService's already-shipped pattern: dbo.M_MapDemandFY mints a new DemandId
    /// every year for the same DemandNo, so a caller's DemandId (e.g. from a stale JWT claim) can
    /// point at last year's row. Falls back to a direct DemandId match for any row whose DemandNo
    /// never got backfilled (confirmed live: a small number of appendix rows have a DemandId with
    /// no matching dbo.M_MapDemandFY entry) - added 2026-08-14, DemandId itself unchanged.
    /// </summary>
    public async Task<List<TEntity>> GetByDemandAndYearAsync(int demandId, string financialYear, CancellationToken ct)
    {
        var demandNo = await ResolveDemandNoAsync(demandId, ct);

        return await _db.Set<TEntity>()
            .Where(e => (demandNo != null && e.DemandNo == demandNo) || (e.DemandNo == null && e.DemandId == demandId))
            .Where(e => e.FinancialYear == financialYear)
            .ToListAsync(ct);
    }

    public Task<TEntity?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Set<TEntity>().SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>
    /// Duplicate-entry check shared by every appendix that needs "unique row per combination of
    /// select fields within a Demand+FinancialYear" validation (client requirement 2026-08-27,
    /// applies generically across the module - Appendix I-A/II single-row-per-Demand, III-A
    /// Demand+Entity, IV/IV-A Demand+Scheme(+SubScheme), VI-C Demand+AutonomousBody, VII-B
    /// Undertaking+MajorHead+TransactionType, and every other appendix's own natural key). Reuses
    /// GetByDemandAndYearAsync (already excludes soft-deleted rows via the global IsDeleted query
    /// filter, and already resolves DemandNo the stable-across-FY-rollover way) rather than a raw
    /// EF query, then evaluates <paramref name="keyMatch"/> in memory - these tables hold at most a
    /// few dozen rows per Demand+FinancialYear, so this is never a real cost, and it lets each
    /// caller's key predicate be arbitrary C# instead of something that has to translate to SQL.
    /// <paramref name="excludeId"/> lets an Update check exclude the row being edited from matching
    /// itself.
    /// </summary>
    public async Task<bool> ExistsMatchingAsync(int demandId, string financialYear, Func<TEntity, bool> keyMatch, int? excludeId, CancellationToken ct)
    {
        var rows = await GetByDemandAndYearAsync(demandId, financialYear, ct);
        return rows.Any(e => (excludeId is null || e.Id != excludeId.Value) && keyMatch(e));
    }

    public async Task<TEntity> AddAsync(TEntity entity, int userId, CancellationToken ct)
    {
        entity.DemandNo = await ResolveDemandNoAsync(entity.DemandId, ct);
        entity.MarkCreated(userId, GetCallerUserName(), GetClientIp());

        _db.Set<TEntity>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity;
    }

    /// <summary>Same resolve-DemandNo-from-DemandId query AutonomousBodyService already uses.</summary>
    private Task<int?> ResolveDemandNoAsync(int demandId, CancellationToken ct) =>
        _db.Demands.Where(d => d.DemandId == demandId).Select(d => (int?)d.DemandNo).FirstOrDefaultAsync(ct);

    /// <summary>Throws InvalidOperationException if the record is already frozen — frozen rows are read-only per the FRS's standard Submit→Freeze workflow (invariant enforced by AppendixEntityBase.EnsureNotFrozen, not this repository).</summary>
    public async Task<TEntity> UpdateAsync(int id, int userId, Action<TEntity> applyChanges, CancellationToken ct)
    {
        var entity = await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} {id} not found.");

        entity.EnsureNotFrozen("modified");

        applyChanges(entity);
        entity.MarkModified(userId, GetCallerUserName(), GetClientIp());

        await _db.SaveChangesAsync(ct);

        return entity;
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} {id} not found.");

        entity.EnsureNotFrozen("deleted");
        entity.MarkDeleted(userId, GetCallerUserName(), GetClientIp());

        await _db.SaveChangesAsync(ct);
    }

    public async Task<TEntity> FreezeAsync(int id, int userId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} {id} not found.");

        entity.MarkFrozen(userId);

        await _db.SaveChangesAsync(ct);

        return entity;
    }

    /// <summary>Same claim AIM's JWT already carries (GenerateJwtToken: sub/UserName/FullName/RoleId/RoleName/...).</summary>
    private string? GetCallerUserName() =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("UserName")?.Value;

    /// <summary>
    /// Same X-Forwarded-For-aware resolution as AIM's AuthenticationService.ExtractClientIpAddress -
    /// requests reach this service through the Gateway, so the direct RemoteIpAddress alone would
    /// just be the Gateway's own address without this header check.
    /// </summary>
    private string? GetClientIp()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return null;
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(rawIp, out _))
            {
                return rawIp;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
