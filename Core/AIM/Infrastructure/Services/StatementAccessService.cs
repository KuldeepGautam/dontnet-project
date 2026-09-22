namespace UBIS.Services.Aim.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Domain.Entities.Legacy;
using UBIS.Services.Aim.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IStatementAccessService"/> per the compliance-brief rule: a Statement
/// Owner bypasses explicit <c>UserDetails</c> mapping and automatically inherits access to every
/// <c>M_SBEDemand</c> row sharing the owned Statement's financial year — the only column common
/// to both <c>M_StmtControl</c> and <c>M_SBEDemand</c> in the verified schema (no direct
/// Statement-to-Demand foreign key exists). See COMPLIANCE_NOTES.md. Added 2026-07-10.
/// </summary>
public class StatementAccessService(AimDbContext db) : IStatementAccessService
{
    public async Task<bool> IsStatementOwnerAsync(int legacyUserId, string financialYear, CancellationToken ct = default) =>
        await db.StmtControls.AsNoTracking()
            .AnyAsync(
                s => s.FinancialYear == financialYear && (s.StmtUserId == legacyUserId || s.StmtUserId1 == legacyUserId),
                ct)
            .ConfigureAwait(false);

    /// <summary>
    /// True if <paramref name="legacyUserId"/> has an explicit non-owner Statement assignment
    /// (<c>dbo.UserId_StmtId_Mapping</c>, added 2026-07) for any Statement in
    /// <paramref name="financialYear"/>. Separate from ownership — <see cref="StmtControl"/>'s
    /// own <c>StmtUserId</c>/<c>StmtUserId1</c> columns are untouched by this table.
    /// </summary>
    public async Task<bool> HasAssignedStatementAsync(int legacyUserId, string financialYear, CancellationToken ct = default) =>
        await (from m in db.UserStmtMappings.AsNoTracking()
               join s in db.StmtControls.AsNoTracking() on m.StmtId equals s.StmtId
               where m.UserId == legacyUserId && m.Active == "Y" && s.FinancialYear == financialYear
               select m.MappingId)
            .AnyAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<SBEDemand>> GetAccessibleDemandsAsync(
        int legacyUserId, string financialYear, CancellationToken ct = default)
    {
        var isOwner = await IsStatementOwnerAsync(legacyUserId, financialYear, ct).ConfigureAwait(false);
        var isAssigned = !isOwner && await HasAssignedStatementAsync(legacyUserId, financialYear, ct).ConfigureAwait(false);

        if (isOwner || isAssigned)
        {
            // Statement Owner or explicitly-assigned Statement user: bypass explicit mapping
            // entirely, inherit every Demand row for the year (same cascade for both — see
            // IStatementAccessService for the "no direct Statement-to-Demand FK exists" rationale).
            return await db.SBEDemands.AsNoTracking()
                .Where(d => d.FinancialYear == financialYear)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        // Neither: fall back to explicit UserDetails ("Profile Access") mapping.
        var grantedDemandIds = await db.UserDetails.AsNoTracking()
            .Where(ud => ud.UserId == legacyUserId)
            .Select(ud => ud.DemandId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (grantedDemandIds.Count == 0)
        {
            return Array.Empty<SBEDemand>();
        }

        return await db.SBEDemands.AsNoTracking()
            .Where(d => d.FinancialYear == financialYear && grantedDemandIds.Contains(d.DemandId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StmtControl>> GetAssignedStatementsAsync(
        int legacyUserId, string financialYear, CancellationToken ct = default)
    {
        var owned = db.StmtControls.AsNoTracking()
            .Where(s => s.FinancialYear == financialYear && (s.StmtUserId == legacyUserId || s.StmtUserId1 == legacyUserId));

        var assigned = from m in db.UserStmtMappings.AsNoTracking()
                       join s in db.StmtControls.AsNoTracking() on m.StmtId equals s.StmtId
                       where m.UserId == legacyUserId && m.Active == "Y" && s.FinancialYear == financialYear
                       select s;

        var results = await owned.Concat(assigned).ToListAsync(ct).ConfigureAwait(false);
        return results
            .GroupBy(s => s.StmtId)
            .Select(g => g.First())
            .OrderBy(s => s.StmtNo)
            .ToList();
    }
}
