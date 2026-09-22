namespace UBIS.Services.Ecl.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.Ecl.Application.DTOs;
using UBIS.Services.Ecl.Application.Interfaces;
using UBIS.Services.Ecl.Domain.Entities;
using UBIS.Services.Ecl.Infrastructure.Persistence;

/// <summary>
/// Bespoke repository for the ECL scheme-outlay workflow — not a generic reused from PreBudget's
/// AppendixDataRepository&lt;T&gt; (the shapes are too different), but it mirrors that class's
/// audit-stamping helper pattern: caller username from the "UserName" JWT claim and an
/// X-Forwarded-For-aware client-IP resolution, both via IHttpContextAccessor since requests reach
/// this service through the Gateway.
/// </summary>
public class EclOutlayRepository : IEclOutlayRepository
{
    private readonly EclDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EclOutlayRepository(EclDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<List<EclSchemeOutlay>> GetByDemandCategorySchemeYearAsync(int? demandId, int? categoryId, int? schemeId, string financialYear, CancellationToken ct)
    {
        var query = _db.SchemeOutlays.Where(e => e.FinancialYear == financialYear);

        if (demandId.HasValue)
        {
            query = query.Where(e => e.DemandId == demandId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId);
        }

        if (schemeId.HasValue)
        {
            query = query.Where(e => e.SchemeId == schemeId);
        }

        return query.OrderByDescending(e => e.RowId).ToListAsync(ct);
    }

    public Task<EclSchemeOutlay?> GetByIdAsync(int rowId, CancellationToken ct) =>
        _db.SchemeOutlays.SingleOrDefaultAsync(e => e.RowId == rowId, ct);

    public async Task<Result<EclSchemeOutlay>> AddAsync(EclSchemeOutlay entity, int userId, CancellationToken ct)
    {
        entity.RecomputeCentralSharePercentage();
        entity.IsActive = true;
        entity.SendToDoe ??= "N";
        entity.ApprovedByDoe ??= "N";
        entity.ReapprovalByDoe ??= "N";
        entity.EntryUserId = userId;
        entity.EntryDateDemand = DateTime.UtcNow;
        entity.IpDemand = GetClientIp();
        entity.UserIdCreatedBy = userId;
        entity.CreatedOnDate = DateTime.UtcNow;

        _db.SchemeOutlays.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    public async Task<Result<EclSchemeOutlay>> UpdateAsync(int rowId, int userId, Action<EclSchemeOutlay> applyChanges, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        if (entity.SendToDoe == "Y" && entity.ApprovedByDoe == "N")
        {
            return Result<EclSchemeOutlay>.Failure(Error.AlreadySentForDoeApproval("This outlay is pending DOE approval and cannot be edited until it is decided or reapproval is requested."));
        }

        applyChanges(entity);
        entity.RecomputeCentralSharePercentage();
        entity.UserIdModifyBy = userId;
        entity.ModifiedOnDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    /// <summary>Soft delete (IsDeleted=1, matching EclSchemeOutlayConfiguration's query filter — the
    /// row stays in the database for audit, just excluded from every normal read). Same
    /// pending-DOE-approval guard as UpdateAsync, so a row mid-review can't be pulled out from
    /// under the DOE.</summary>
    public async Task<Result<bool>> DeleteAsync(int rowId, int userId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<bool>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        if (entity.SendToDoe == "Y" && entity.ApprovedByDoe == "N")
        {
            return Result<bool>.Failure(Error.AlreadySentForDoeApproval("This outlay is pending DOE approval and cannot be deleted until it is decided or reapproval is requested."));
        }

        entity.IsDeleted = true;
        entity.DeletedByUserId = userId;
        entity.DeletedByIp = GetClientIp();
        entity.DeletedOnDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<EclSchemeOutlay>> SubmitForApprovalAsync(int rowId, int userId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        try
        {
            entity.SubmitForDoeApproval();
        }
        catch (InvalidOperationException ex) when (entity.ApprovedByDoe == "Y")
        {
            return Result<EclSchemeOutlay>.Failure(Error.AlreadyApprovedByDoe(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result<EclSchemeOutlay>.Failure(Error.AlreadyPendingDoeApproval(ex.Message));
        }

        entity.UserIdModifyBy = userId;
        entity.ModifiedOnDate = DateTime.UtcNow;
        entity.IpDemand = GetClientIp();

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    public async Task<Result<EclSchemeOutlay>> ApproveAsync(int rowId, int approveUserId, string? doeRemarks, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        try
        {
            entity.ApproveByDoe(approveUserId, GetClientIp(), doeRemarks);
        }
        catch (InvalidOperationException ex)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotPendingDoeApproval(ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    public async Task<Result<EclSchemeOutlay>> RejectAsync(int rowId, int approveUserId, string? doeRemarks, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        if (string.IsNullOrWhiteSpace(doeRemarks))
        {
            return Result<EclSchemeOutlay>.Failure(Error.RejectionRequiresRemarks());
        }

        try
        {
            entity.RejectByDoe(approveUserId, GetClientIp(), doeRemarks);
        }
        catch (InvalidOperationException ex)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotPendingDoeApproval(ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    public async Task<Result<EclSchemeOutlay>> RequestReapprovalAsync(int rowId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        try
        {
            entity.RequestReapproval();
        }
        catch (InvalidOperationException ex)
        {
            return Result<EclSchemeOutlay>.Failure(Error.ReapprovalNotAllowed(ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    /// <summary>Records the FY1-10 actuals grid AND writes one dbo.ECL_T_Actuals_Log audit row in the same save.</summary>
    public async Task<Result<EclSchemeOutlay>> RecordActualsAsync(int rowId, decimal?[] actuals, int userId, CancellationToken ct)
    {
        var entity = await GetByIdAsync(rowId, ct);
        if (entity is null)
        {
            return Result<EclSchemeOutlay>.Failure(Error.NotFound(nameof(EclSchemeOutlay), rowId));
        }

        var ip = GetClientIp();
        var isFirstEntry = entity.ActualsEntryDate is null;

        entity.RecordActuals(actuals, userId, ip);

        _db.ActualsLog.Add(new EclActualsLogEntry
        {
            RowId = entity.RowId,
            FinancialYear = entity.FinancialYear,
            CategoryId = entity.CategoryId,
            SubCategoryId = entity.SubCategoryId,
            DemandId = entity.DemandId,
            SchemeId = entity.SchemeId,
            Fy1Actuals = actuals[0],
            Fy2Actuals = actuals[1],
            Fy3Actuals = actuals[2],
            Fy4Actuals = actuals[3],
            Fy5Actuals = actuals[4],
            Fy6Actuals = actuals[5],
            Fy7Actuals = actuals[6],
            Fy8Actuals = actuals[7],
            Fy9Actuals = actuals[8],
            Fy10Actuals = actuals[9],
            ActualsUserId = userId,
            ActualsIp = ip,
            ActualsEntryDate = entity.ActualsEntryDate,
            Action = isFirstEntry ? EclActualsLogEntry.Actions.Insert : EclActualsLogEntry.Actions.Update,
            ModifiedDate = DateTime.UtcNow,
            ModifiedIp = ip,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow,
            IsDeleted = false
        });

        await _db.SaveChangesAsync(ct);

        return Result<EclSchemeOutlay>.Success(entity);
    }

    public Task<List<EclCategory>> GetCategoriesAsync(string financialYear, CancellationToken ct) =>
        _db.Categories
            .Where(c => c.FinancialYear == financialYear && c.IsActive && (c.SerialNo == "II" || c.SerialNo == "IV"))
            .OrderBy(c => c.SerialNo)
            .ToListAsync(ct);

    /// <summary>Client-corrected 2026-08-20: schemes for AddSchemeOutlay's Scheme dropdown must be
    /// filtered by BOTH Demand and Category — initial correction was "select str(SchemeSrNo) + ' - '
    /// + SchemeName from M_Scheme where demandid=@DemandId and SchemeSrNo &lt;&gt;0 order by
    /// SchemeSrNo", then the client clarified Category must also apply. Previously filtered by
    /// CategoryId alone, which never excluded the SchemeSrNo=0 placeholder rows and ignored Demand
    /// entirely (so schemes from every Demand under a Category leaked into the dropdown).</summary>
    public Task<List<EclScheme>> GetSchemesAsync(int demandId, int categoryId, CancellationToken ct) =>
        _db.Schemes
            .Where(s => s.DemandId == demandId && s.CategoryId == categoryId && s.SchemeSrNo != 0 && s.IsActive)
            .OrderBy(s => s.SchemeSrNo)
            .ToListAsync(ct);

    /// <summary>Umbrella-scheme options for ECL Master's "Add Schemes" screen — client-corrected
    /// 2026-08-19: "select * from M_UmbScheme where CategoryId = @Categoryid and Demandid=@DemandId".
    /// dbo.M_UmbScheme is the REAL umbrella-scheme master table (added 2026-08-19, structure/data
    /// matched to the real BIMSDemo.dbo.M_UmbScheme) — this replaces an earlier, wrong
    /// implementation that reused dbo.M_Scheme.IsUmbrella, which isn't the umbrella-scheme master
    /// table at all. Active is this table's own char "Y"/"N" column (not this repo's usual bit
    /// IsActive).</summary>
    public Task<List<EclUmbScheme>> GetUmbrellaSchemeOptionsAsync(int categoryId, int demandId, CancellationToken ct) =>
        _db.UmbSchemes
            .Where(u => u.CategoryId == categoryId && u.DemandId == demandId && u.Active == "Y")
            .OrderBy(u => u.DisplaySeqNo)
            .ToListAsync(ct);

    /// <summary>Bulk name lookup by SchemeId, spanning any category — added 2026-08-19 for
    /// GetOutlays, whose rows can belong to multiple categories at once (it's queried with
    /// categoryId=null for the saved-grid view), unlike GetSchemesAsync's single-category scope
    /// which was being (incorrectly) reused client-side for this and silently missed any row
    /// outside the currently-selected category.</summary>
    public Task<Dictionary<int, string>> GetSchemeNamesByIdsAsync(IEnumerable<int> schemeIds, CancellationToken ct) =>
        _db.Schemes
            .Where(s => schemeIds.Contains(s.SchemeId))
            .Select(s => new { s.SchemeId, s.SchemeName })
            .ToDictionaryAsync(s => s.SchemeId, s => s.SchemeName, ct);

    /// <summary>Bulk (SchemeName, SchemeSrNo) lookup by SchemeId — added 2026-08-19 for grid display
    /// ("SchemeSrNo - SchemeName") on the DOE approval/reapproval and report grids, which previously
    /// either showed a raw "Scheme #123" fallback with no name lookup at all, or (the report grid)
    /// only a name with no SrNo.</summary>
    public async Task<Dictionary<int, (string SchemeName, int? SchemeSrNo)>> GetSchemeInfoByIdsAsync(IEnumerable<int> schemeIds, CancellationToken ct)
    {
        var rows = await _db.Schemes
            .Where(s => schemeIds.Contains(s.SchemeId))
            .Select(s => new { s.SchemeId, s.SchemeName, s.SchemeSrNo })
            .ToListAsync(ct);
        return rows.ToDictionary(s => s.SchemeId, s => (s.SchemeName, s.SchemeSrNo));
    }

    /// <summary>Bulk (DemandNo, DemandName) lookup by DemandId — added 2026-08-19 for grid display
    /// ("DemandNo - DemandName") on the same grids as GetSchemeInfoByIdsAsync. Keyed by DemandId
    /// (the per-year id ECL_T_Outlay.DemandID actually stores), same as ResolveDemandNoAsync.</summary>
    public async Task<Dictionary<int, (int? DemandNo, string? DemandName)>> GetDemandInfoByIdsAsync(IEnumerable<int> demandIds, CancellationToken ct)
    {
        var rows = await _db.Demands
            .Where(d => demandIds.Contains(d.DemandId))
            .Select(d => new { d.DemandId, d.DemandNo, d.DemandName })
            .ToListAsync(ct);
        return rows.ToDictionary(d => d.DemandId, d => ((int?)d.DemandNo, d.DemandName));
    }

    /// <summary>
    /// Creates a new dbo.M_Scheme row (added 2026-08-18, closing the gap flagged when the ECL
    /// Master "Add Schemes" screen was first built). SchemeSrNo is assigned as the next available
    /// serial number within the category, matching the ordering GetSchemesAsync already relies on.
    /// Note: umbrellaSchemeId is accepted but not persisted — dbo.M_Scheme has no parent-scheme
    /// column (IsUmbrella only records whether THIS scheme is itself an umbrella grouping, not a
    /// link to one); adding such a column would be new, unrequested schema, so the caller is told
    /// this explicitly via the returned entity rather than silently dropped.
    /// </summary>
    public async Task<Result<EclScheme>> CreateSchemeAsync(CreateSchemeRequestDto request, int userId, CancellationToken ct)
    {
        var nextSrNo = await _db.Schemes
            .Where(s => s.CategoryId == request.CategoryId)
            .Select(s => (int?)s.SchemeSrNo)
            .MaxAsync(ct) ?? 0;

        var entity = new EclScheme
        {
            DemandId = request.DemandId,
            CategoryId = request.CategoryId,
            IsUmbrella = request.IsUmbrella,
            UmbSchemeId = request.IsUmbrella ? null : request.UmbrellaSchemeId,
            SchemeName = request.SchemeNameEnglish,
            HSchemeName = request.SchemeNameHindi,
            SchemeSrNo = nextSrNo + 1,
            IsActive = true,
            IsDeleted = false,
            Ip = GetClientIp(),
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };

        _db.Schemes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<EclScheme>.Success(entity);
    }

    public Task<List<EclApprovalAuthority>> GetApprovalAuthoritiesAsync(CancellationToken ct) =>
        _db.ApprovalAuthorities
            .Where(a => a.IsDropdownVisible)
            .OrderBy(a => a.DisplaySequenceNo)
            .ToListAsync(ct);

    public Task<List<EclAppraiseAuthority>> GetAppraiseAuthoritiesAsync(CancellationToken ct) =>
        _db.AppraiseAuthorities
            .Where(a => a.IsDropdownVisible)
            .OrderBy(a => a.DisplaySequenceNo)
            .ToListAsync(ct);

    /// <summary>"On selecting non-other value, the textbox blanks and disappears. If other textbox
    /// value got saved, the master table should be inserted with new value with isdropdown visible
    /// 0" (client requirement 2026-08-25). Case-insensitive match against an existing row first —
    /// re-typing an already-promoted "Others" value (or a name that happens to match a visible
    /// dropdown entry) must resolve to the SAME row, not create a duplicate every time.</summary>
    public async Task<int?> EnsureApprovalAuthorityAsync(string? name, int userId, CancellationToken ct)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        var existing = await _db.ApprovalAuthorities
            .FirstOrDefaultAsync(a => a.AuthorityName.ToLower() == trimmed.ToLower(), ct);
        if (existing != null)
        {
            return existing.AuthorityId;
        }

        var entity = new EclApprovalAuthority
        {
            AuthorityName = trimmed,
            IsDropdownVisible = false,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };
        _db.ApprovalAuthorities.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.AuthorityId;
    }

    /// <summary>Same "Others" promotion as EnsureApprovalAuthorityAsync, against dbo.M_ECLAppraiseAuthority.</summary>
    public async Task<int?> EnsureAppraiseAuthorityAsync(string? name, int userId, CancellationToken ct)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        var existing = await _db.AppraiseAuthorities
            .FirstOrDefaultAsync(a => a.AppraiseName.ToLower() == trimmed.ToLower(), ct);
        if (existing != null)
        {
            return existing.AppraiseId;
        }

        var entity = new EclAppraiseAuthority
        {
            AppraiseName = trimmed,
            IsDropdownVisible = false,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };
        _db.AppraiseAuthorities.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.AppraiseId;
    }

    public Task<EclConfig?> GetConfigAsync(CancellationToken ct) =>
        _db.Configs.OrderBy(c => c.Id).FirstOrDefaultAsync(ct);

    /// <summary>Replaces GetConfigAsync as the source for Add Scheme Outlay's selectable/Outlay-
    /// column years (client request 2026-08-19) — see EclFinanceCommission's doc comment. Takes the
    /// highest FinancialCommissionNo as "current" (only one row is expected in practice).</summary>
    public Task<EclFinanceCommission?> GetFinanceCommissionAsync(CancellationToken ct) =>
        _db.FinanceCommissions.OrderByDescending(f => f.FinancialCommissionNo).FirstOrDefaultAsync(ct);

    public async Task<int> ResolveDemandNoAsync(int demandId, CancellationToken ct)
    {
        var demandNo = await _db.Demands.Where(d => d.DemandId == demandId).Select(d => (int?)d.DemandNo).FirstOrDefaultAsync(ct);
        return demandNo ?? demandId;
    }

    /// <summary>Same X-Forwarded-For-aware resolution used by PreBudget's AppendixDataRepository&lt;T&gt; and AIM's AuthenticationService.ExtractClientIpAddress — requests reach this service through the Gateway.</summary>
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
