namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Security;

public class AutonomousBodyService : IAutonomousBodyService
{
    private readonly PreBudgetDbContext _db;
    private readonly AutonomousBodyAdminRoleOptions _adminRoleOptions;
    private readonly AutonomousBodyCreatorRoleOptions _creatorRoleOptions;

    public AutonomousBodyService(
        PreBudgetDbContext db,
        IOptions<AutonomousBodyAdminRoleOptions> adminRoleOptions,
        IOptions<AutonomousBodyCreatorRoleOptions> creatorRoleOptions)
    {
        _db = db;
        _adminRoleOptions = adminRoleOptions.Value;
        _creatorRoleOptions = creatorRoleOptions.Value;
    }

    public async Task<IReadOnlyList<AutonomousBodyDto>> GetAutonomousBodiesAsync(int demandId, string financialYear, CancellationToken ct)
    {
        // Client review 2026-08-06: filter by DemandNo (stable across financial years), resolved
        // from whatever DemandId came in - not DemandId directly, since AIM's role-assignment
        // DemandId claims can go stale across FY rollovers (dbo.M_Demand mints a new DemandId every
        // year for the same DemandNo) and return the wrong or no demand at all.
        var demandNo = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.DemandNo)
            .FirstOrDefaultAsync(ct);

        var bodies = await _db.AutonomousBodies
            .Where(a => a.DemandNo == demandNo && a.FinancialYear == financialYear && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        return bodies.Select(a => new AutonomousBodyDto
        {
            AutonomousBodyId = a.AutonomousBodyId,
            DemandId = a.DemandId,
            Code = a.Code,
            Name = a.Name,
            HName = a.HName
        }).ToList();
    }

    public async Task<IReadOnlyList<AutonomousBodyDto>> GetAutonomousBodiesForAppendixVAAsync(int demandId, string financialYear, CancellationToken ct)
    {
        // "Appendix VA autonomous body dropdown not bound, bind it with M_AutonomousBody table
        // based on demand and financial year" (client requirement, 2026-08-25) - was deliberately
        // NOT financial-year-scoped per an earlier 2026-08-06 decision (DemandNo only, grouped by
        // Name across every year to dedupe); now matches GetAutonomousBodiesAsync's own convention
        // (VI-C/VI-E's dropdown) - DemandNo (stable across the DemandId-per-year rollover, same
        // reason as GetAutonomousBodiesAsync above) AND FinancialYear AND IsActive. The GroupBy-by-
        // Name dedup is kept defensively (each option still carries a real AutonomousBodyId to bind
        // to, since the entry form saves against that FK) even though a clean (DemandNo,
        // FinancialYear) filter should rarely produce more than one row per Name.
        var demandNo = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.DemandNo)
            .FirstOrDefaultAsync(ct);

        var rawBodies = await _db.AutonomousBodies
            .Where(a => a.DemandNo == demandNo && a.FinancialYear == financialYear && a.IsActive)
            .ToListAsync(ct);

        // Client report 2026-08-27: exact-string GroupBy(a => a.Name) still let two visually
        // identical entries through for the same real body ("Central Wool Development Board" vs.
        // "Central  Wool Development Board" - a stray double space from data entry, confirmed
        // directly against dbo.M_AutonomousBody). Group on a whitespace-normalized key instead so a
        // typo like that doesn't defeat the dedup; the option's own label still comes from
        // whichever real row wins (lowest AutonomousBodyId), untouched.
        var bodies = rawBodies
            .GroupBy(a => NormalizeNameForDedup(a.Name), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(a => a.AutonomousBodyId).First())
            .ToList();

        return bodies
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new AutonomousBodyDto
            {
                AutonomousBodyId = a.AutonomousBodyId,
                DemandId = a.DemandId,
                Code = a.Code,
                Name = a.Name,
                HName = a.HName
            }).ToList();
    }

    /// <summary>Direct add - creates the M_AutonomousBody row VI-C/VI-E's dropdowns
    /// (GetAutonomousBodiesAsync) read from immediately, no request/approval step. Originally
    /// Administrator-only; opened up to Single Demand Users too (client requirement, 2026-08-31:
    /// "it should not request Autonomous Body, it should add it") - gated by
    /// AutonomousBodyCreatorRoleOptions, deliberately separate from AutonomousBodyAdminRoleOptions
    /// (EnsureAdmin below), which still gates the request review/approval workflow only.</summary>
    public async Task<AutonomousBodyDto> CreateAutonomousBodyAsync(CreateAutonomousBodyDto request, int userId, string callerRoleName, CancellationToken ct)
    {
        EnsureCanCreate(callerRoleName);

        var demandNo = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == request.DemandId)
            .Select(d => d.DemandNo)
            .FirstOrDefaultAsync(ct);

        var body = new AutonomousBody
        {
            FinancialYear = request.FinancialYear,
            DemandId = request.DemandId,
            DemandNo = demandNo,
            Name = request.Name,
            HName = request.HName,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };
        _db.AutonomousBodies.Add(body);
        await _db.SaveChangesAsync(ct);

        return new AutonomousBodyDto
        {
            AutonomousBodyId = body.AutonomousBodyId,
            DemandId = body.DemandId,
            Code = body.Code,
            Name = body.Name,
            HName = body.HName
        };
    }

    public async Task<AutonomousBodyRequestDto> RequestNewAutonomousBodyAsync(CreateAutonomousBodyRequestDto request, int userId, CancellationToken ct)
    {
        var entity = new AutonomousBodyRequest
        {
            DemandId = request.DemandId,
            FinancialYear = request.FinancialYear,
            RequestedName = request.RequestedName,
            RequestedByUserId = userId,
            RequestedAtUtc = DateTime.UtcNow,
            Status = AutonomousBodyRequestStatus.Pending
        };

        _db.AutonomousBodyRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<AutonomousBodyRequestDto>> GetPendingRequestsAsync(string callerRoleName, CancellationToken ct)
    {
        EnsureAdmin(callerRoleName);

        var requests = await _db.AutonomousBodyRequests
            .Where(r => r.Status == AutonomousBodyRequestStatus.Pending)
            .OrderBy(r => r.RequestedAtUtc)
            .ToListAsync(ct);

        return requests.Select(ToDto).ToList();
    }

    public async Task<AutonomousBodyRequestDto> ReviewRequestAsync(int requestId, ReviewAutonomousBodyRequestDto review, int reviewerUserId, string callerRoleName, CancellationToken ct)
    {
        EnsureAdmin(callerRoleName);

        var request = await _db.AutonomousBodyRequests.SingleOrDefaultAsync(r => r.RequestId == requestId, ct)
            ?? throw new InvalidOperationException($"Autonomous Body request {requestId} not found.");

        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = DateTime.UtcNow;
        request.ReviewRemarks = review.ReviewRemarks;

        if (review.Approve)
        {
            // GetAutonomousBodiesAsync (VI-C/VI-E's dropdown) filters by DemandNo, not DemandId -
            // this must be populated or an approved body would never actually show up there (bug
            // found 2026-08-10 while adding the direct-add endpoint below).
            var demandNo = await _db.Demands.AsNoTracking()
                .Where(d => d.DemandId == request.DemandId)
                .Select(d => d.DemandNo)
                .FirstOrDefaultAsync(ct);

            var body = new AutonomousBody
            {
                FinancialYear = request.FinancialYear,
                DemandId = request.DemandId,
                DemandNo = demandNo,
                Code = review.Code,
                Name = request.RequestedName,
                IsActive = true,
                UserIdCreatedBy = reviewerUserId,
                CreatedOnDate = DateTime.UtcNow
            };
            _db.AutonomousBodies.Add(body);

            request.Status = AutonomousBodyRequestStatus.Approved;
            await _db.SaveChangesAsync(ct);

            request.ApprovedAutonomousBodyId = body.AutonomousBodyId;
        }
        else
        {
            request.Status = AutonomousBodyRequestStatus.Rejected;
        }

        await _db.SaveChangesAsync(ct);

        return ToDto(request);
    }

    /// <summary>Collapses leading/trailing/internal whitespace runs to single spaces so a stray
    /// double-space typo in dbo.M_AutonomousBody.Name doesn't produce a visually-duplicate dropdown
    /// entry (see GetAutonomousBodiesForAppendixVAAsync).</summary>
    private static string NormalizeNameForDedup(string? name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ");

    private void EnsureAdmin(string callerRoleName)
    {
        if (!_adminRoleOptions.AllowedRoleNames.Contains(callerRoleName, StringComparer.OrdinalIgnoreCase))
        {
            throw new AutonomousBodyAccessDeniedException();
        }
    }

    private void EnsureCanCreate(string callerRoleName)
    {
        if (!_creatorRoleOptions.AllowedRoleNames.Contains(callerRoleName, StringComparer.OrdinalIgnoreCase))
        {
            throw new AutonomousBodyAccessDeniedException();
        }
    }

    private static AutonomousBodyRequestDto ToDto(AutonomousBodyRequest r) => new()
    {
        RequestId = r.RequestId,
        DemandId = r.DemandId,
        FinancialYear = r.FinancialYear,
        RequestedName = r.RequestedName,
        RequestedByUserId = r.RequestedByUserId,
        RequestedAtUtc = r.RequestedAtUtc,
        Status = r.Status,
        ReviewRemarks = r.ReviewRemarks
    };
}
