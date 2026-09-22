namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Infrastructure.Persistence;

public class AppendixService : IAppendixService
{
    private readonly PreBudgetDbContext _db;
    private readonly IAimContactsClient _aimContactsClient;
    private readonly INotificationPublisher _notificationPublisher;

    public AppendixService(PreBudgetDbContext db, IAimContactsClient aimContactsClient, INotificationPublisher notificationPublisher)
    {
        _db = db;
        _aimContactsClient = aimContactsClient;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<IReadOnlyList<AppendixDto>> GetAppendicesAsync(string financialYear, CancellationToken ct)
    {
        var appendices = await _db.Appendices
            .Where(a => a.FinancialYear == financialYear && a.IsActive)
            .OrderBy(a => a.DisplaySequence)
            .ToListAsync(ct);

        return appendices.Select(a => new AppendixDto
        {
            AppendixId = a.AppendixId,
            Code = a.Code,
            Name = a.Name,
            HName = a.HName,
            DisplaySequence = a.DisplaySequence
        }).ToList();
    }

    public async Task<IReadOnlyList<AppendixWithStatusDto>> GetAppendicesWithStatusAsync(int demandId, string financialYear, CancellationToken ct)
    {
        var appendices = await _db.Appendices
            .Where(a => a.FinancialYear == financialYear && a.IsActive)
            .OrderBy(a => a.DisplaySequence)
            .ToListAsync(ct);

        var statuses = await _db.DemandAllocations
            .Where(s => s.DemandId == demandId && s.FinancialYear == financialYear)
            .ToDictionaryAsync(s => s.AppendixId, ct);

        var today = DateTime.UtcNow.Date;
        return appendices.Select(a =>
        {
            statuses.TryGetValue(a.AppendixId, out var status);
            var isFrozen = status?.FrozenAtUtc is not null;
            return new AppendixWithStatusDto
            {
                AppendixId = a.AppendixId,
                Code = a.Code,
                Name = a.Name,
                Remarks = a.Remarks,
                ParaNo = a.ParaNo,
                DisplaySequence = a.DisplaySequence,
                IsFrozen = isFrozen,
                IsNilSubmitted = !isFrozen && status?.NilRemarks is not null,
                TargetDate = status?.TargetDate,
                IsTargetDateExpired = !isFrozen && status?.TargetDate is not null && status.TargetDate.Value.Date < today
            };
        }).ToList();
    }

    public async Task SetNilSubmissionAsync(SetNilSubmissionDto request, int userId, CancellationToken ct)
    {
        var status = await GetOrCreateStatusAsync(request.DemandId, request.AppendixId, request.FinancialYear, userId, ct);

        if (status.FrozenAtUtc is not null)
        {
            throw new InvalidOperationException(
                $"Appendix {request.AppendixId} for demand {request.DemandId} ({request.FinancialYear}) is frozen and cannot be changed to Nil Submitted.");
        }

        // Bug found via live DB round-trip verification (2026-09-08, while relocating the Nil
        // button in UBIS_Web from the drawer footer to the page header): every "Nil" button across
        // every appendix (I/I-A and this VI-A/B/C/D/E family) confirms via a plain yes/no dialog
        // with no remarks textbox, so request.NilRemarks is always null - and since
        // IsNilSubmitted is computed below (and in GetAllocationsAsync above) purely as
        // "NilRemarks is not null", clicking Nil never actually flipped IsNilSubmitted to true; it
        // only ever touched ModifiedOnDate. Default to a fixed remarks string the same way
        // AllocateAppendicesAsync's own Allocation-edit path already does ("Marked Nil via
        // Allocation edit" below) so a plain confirm click actually persists as Nil.
        status.NilRemarks = string.IsNullOrWhiteSpace(request.NilRemarks) ? "Marked Nil" : request.NilRemarks;
        status.UserIdModifyBy = userId;
        status.ModifiedOnDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task FreezeSubmissionAsync(int demandId, int appendixId, string financialYear, int userId, CancellationToken ct)
    {
        var status = await GetOrCreateStatusAsync(demandId, appendixId, financialYear, userId, ct);
        status.FrozenAtUtc = DateTime.UtcNow;
        status.FrozenByUserId = userId;
        status.UserIdModifyBy = userId;
        status.ModifiedOnDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<AllocationResultDto> AllocateAsync(AllocateAppendixDto request, int userId, string bearerToken, CancellationToken ct)
    {
        var appendixIds = request.AppendixId.HasValue
            ? new List<int> { request.AppendixId.Value }
            : await _db.Appendices
                .Where(a => a.FinancialYear == request.FinancialYear && a.IsActive)
                .Select(a => a.AppendixId)
                .ToListAsync(ct);

        var allocatedDemandIds = new HashSet<int>();
        var skippedFrozenDemandIds = new HashSet<int>();

        foreach (var demandId in request.DemandIds)
        {
            var anyAllocated = false;
            foreach (var appendixId in appendixIds)
            {
                var status = await GetOrCreateStatusAsync(demandId, appendixId, request.FinancialYear, userId, ct);
                if (status.FrozenAtUtc is not null)
                {
                    // Can't re-allocate a frozen appendix - matches FreezeSubmissionAsync's own guard spirit.
                    continue;
                }

                anyAllocated = true;
                status.TargetDate = request.TargetDate;
                status.UserIdModifyBy = userId;
                status.ModifiedOnDate = DateTime.UtcNow;
            }

            if (anyAllocated)
            {
                allocatedDemandIds.Add(demandId);
            }
            else if (appendixIds.Count > 0)
            {
                skippedFrozenDemandIds.Add(demandId);
            }
        }

        await _db.SaveChangesAsync(ct);

        if (allocatedDemandIds.Count > 0)
        {
            await DispatchAllocationNotificationsAsync(request, allocatedDemandIds.ToList(), bearerToken, ct);
        }

        return new AllocationResultDto
        {
            DemandsAllocated = allocatedDemandIds.Count,
            AppendixesAllocated = appendixIds.Count,
            SkippedFrozenDemandIds = skippedFrozenDemandIds.ToList()
        };
    }

    public async Task<IReadOnlyList<AllocationListItemDto>> GetAllocationsAsync(int? appendixId, string financialYear, CancellationToken ct)
    {
        var appendixIds = appendixId.HasValue
            ? new List<int> { appendixId.Value }
            : await _db.Appendices
                .Where(a => a.FinancialYear == financialYear && a.IsActive)
                .Select(a => a.AppendixId)
                .ToListAsync(ct);

        var statuses = await _db.DemandAllocations
            .Where(s => appendixIds.Contains(s.AppendixId) && s.FinancialYear == financialYear && s.TargetDate != null)
            .ToListAsync(ct);

        if (statuses.Count == 0)
        {
            return Array.Empty<AllocationListItemDto>();
        }

        var demandIds = statuses.Select(s => s.DemandId).Distinct().ToList();
        var demands = await _db.Demands
            .Where(d => demandIds.Contains(d.DemandId) && d.FinancialYear == financialYear)
            .ToDictionaryAsync(d => d.DemandId, d => d, ct);

        return statuses
            .GroupBy(s => s.DemandId)
            .Select(g =>
            {
                demands.TryGetValue(g.Key, out var demand);
                return new AllocationListItemDto
                {
                    DemandId = g.Key,
                    DemandNo = demand?.DemandNo ?? 0,
                    DemandName = demand?.DemandName ?? string.Empty,
                    TargetDate = g.Max(s => s.TargetDate),
                    IsFrozen = g.All(s => s.FrozenAtUtc is not null),
                    IsNilSubmitted = g.All(s => s.FrozenAtUtc is null && s.NilRemarks is not null)
                };
            })
            .OrderBy(r => r.DemandNo)
            .ToList();
    }

    /// <summary>
    /// Resolves recipients per checked category and fire-and-forget publishes their notifications.
    /// Demand-scoped roles (Budget Officer/CCA/FA) are resolved once per allocated Demand; org-wide
    /// Budget/MOF roles (Budget Division Officer/Section User) are resolved once regardless of how
    /// many Demands were allocated. Never throws - a notification failure must not fail the
    /// allocation itself (already committed by the time this runs).
    /// </summary>
    private async Task DispatchAllocationNotificationsAsync(AllocateAppendixDto request, List<int> demandIds, string bearerToken, CancellationToken ct)
    {
        var demandScopedCategories = new (string RoleName, NotificationChannelFlags Flags)[]
        {
            ("Budget Officer", request.NotifyDemandBudgetOfficer),
            ("CCA", request.NotifyDemandCca),
            ("FA", request.NotifyDemandFa)
        };
        var orgWideCategories = new (string RoleName, NotificationChannelFlags Flags)[]
        {
            ("Budget Division Officer", request.NotifyBudgetDivisionOfficer),
            ("Section User", request.NotifySectionUser)
        };

        var flagsByRoleName = demandScopedCategories.Concat(orgWideCategories)
            .ToDictionary(c => c.RoleName, c => c.Flags, StringComparer.OrdinalIgnoreCase);

        var demandScopedRoleNames = demandScopedCategories.Where(c => c.Flags.Email || c.Flags.Sms).Select(c => c.RoleName).ToList();
        var orgWideRoleNames = orgWideCategories.Where(c => c.Flags.Email || c.Flags.Sms).Select(c => c.RoleName).ToList();

        var recipients = new List<RecipientContactDto>();

        if (orgWideRoleNames.Count > 0)
        {
            recipients.AddRange(await _aimContactsClient.GetContactsByRoleAsync(orgWideRoleNames, demandId: null, bearerToken, ct));
        }

        if (demandScopedRoleNames.Count > 0)
        {
            foreach (var demandId in demandIds)
            {
                recipients.AddRange(await _aimContactsClient.GetContactsByRoleAsync(demandScopedRoleNames, demandId, bearerToken, ct));
            }
        }

        var subject = $"Pre-Budget Allocation: Target Date {request.TargetDate:dd-MMM-yyyy}";
        var body = $"An appendix allocation has been made for your Demand with Target Date {request.TargetDate:dd-MMM-yyyy}. Please complete data entry before this date.";

        foreach (var recipient in recipients)
        {
            if (!flagsByRoleName.TryGetValue(recipient.RoleName, out var flags))
            {
                continue;
            }

            if (flags.Email && !string.IsNullOrWhiteSpace(recipient.Email))
            {
                await _notificationPublisher.PublishEmailAsync(recipient.Email!, recipient.Name, subject, body, ct);
            }

            if (flags.Sms && !string.IsNullOrWhiteSpace(recipient.Mobile))
            {
                await _notificationPublisher.PublishSmsAsync(recipient.Mobile!, body, ct);
            }
        }
    }

    /// <summary>Row-level edit for one Demand's allocation (client requirement 2026-08-28) - see
    /// UpdateAllocationDto's doc comment for why this applies to every DemandAllocation row in the
    /// same appendixId set GetAllocationsAsync itself aggregates over.</summary>
    public async Task UpdateAllocationAsync(UpdateAllocationDto request, int userId, CancellationToken ct)
    {
        var appendixIds = request.AppendixId.HasValue
            ? new List<int> { request.AppendixId.Value }
            : await _db.Appendices
                .Where(a => a.FinancialYear == request.FinancialYear && a.IsActive)
                .Select(a => a.AppendixId)
                .ToListAsync(ct);

        foreach (var appendixId in appendixIds)
        {
            var status = await GetOrCreateStatusAsync(request.DemandId, appendixId, request.FinancialYear, userId, ct);
            status.TargetDate = request.TargetDate;

            // Mutually exclusive, matching GetAllocationsAsync's own aggregate semantics
            // (IsFrozen = FrozenAtUtc set; IsNilSubmitted = not frozen AND NilRemarks set).
            if (request.IsFrozen)
            {
                status.FrozenAtUtc ??= DateTime.UtcNow;
                status.FrozenByUserId ??= userId;
                status.NilRemarks = null;
            }
            else
            {
                status.FrozenAtUtc = null;
                status.FrozenByUserId = null;
                status.NilRemarks = request.IsNilSubmitted ? (status.NilRemarks ?? "Marked Nil via Allocation edit") : null;
            }

            status.UserIdModifyBy = userId;
            status.ModifiedOnDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<int>> GetAllocatedDemandIdsAsync(IReadOnlyList<int> demandIds, string financialYear, CancellationToken ct)
    {
        if (demandIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        // DISTINCT, not a plain join - DemandAllocation is one row per (Demand, Appendix,
        // FinancialYear), so a Demand with N allocated Appendixes would otherwise contribute N
        // rows here (see this method's own interface doc comment for the bug this fixes).
        return await _db.DemandAllocations
            .Where(a => demandIds.Contains(a.DemandId) && a.FinancialYear == financialYear && !a.IsDeleted)
            .Select(a => a.DemandId)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<DemandAllocation> GetOrCreateStatusAsync(int demandId, int appendixId, string financialYear, int userId, CancellationToken ct)
    {
        var status = await _db.DemandAllocations.SingleOrDefaultAsync(
            s => s.DemandId == demandId && s.AppendixId == appendixId && s.FinancialYear == financialYear, ct);

        if (status is not null)
        {
            return status;
        }

        status = new DemandAllocation
        {
            DemandId = demandId,
            AppendixId = appendixId,
            FinancialYear = financialYear,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };
        _db.DemandAllocations.Add(status);

        return status;
    }
}
