namespace UBIS.Services.PreBudget.Infrastructure.Persistence;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Reference;
using UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;
using UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

/// <summary>
/// DB-first, no EF migrations — same shared UBIS-Dev database every other microservice in this
/// solution points at. Tables are created via SQL Queries\new-tables\PreBudget_CreateTables_01_
/// Master.sql and _02_Appendices.sql (DBA-owned scripts), not EF Core migrations.
/// </summary>
public class PreBudgetDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PreBudgetDbContext(DbContextOptions<PreBudgetDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Master / reference
    public DbSet<Appendix> Appendices { get; set; } = null!;
    public DbSet<AutonomousBody> AutonomousBodies { get; set; } = null!;
    public DbSet<AutonomousBodyRequest> AutonomousBodyRequests { get; set; } = null!;

    // Workflow
    public DbSet<DemandAllocation> DemandAllocations { get; set; } = null!;
    public DbSet<Remark> Remarks { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLog { get; set; } = null!;

    // Appendices
    public DbSet<AppendixBudgetExpenditureTrend> AppendixIBudgetExpenditureTrends { get; set; } = null!;
    public DbSet<AppendixProjectedDemand> AppendixIAProjectedDemands { get; set; } = null!;
    public DbSet<AppendixQuarterlyExpenditurePlan> AppendixIIQuarterlyExpenditurePlans { get; set; } = null!;
    public DbSet<AppendixCnaSnaBalance> AppendixIIICnaSnaBalances { get; set; } = null!;
    public DbSet<AppendixTsaAssignment> AppendixIIIATsaAssignments { get; set; } = null!;
    public DbSet<AppendixEstimatesOfSchemes> AppendixIVEstimatesOfSchemes { get; set; } = null!;
    public DbSet<AppendixScspExpenditure> AppendixIVAScspExpenditures { get; set; } = null!;
    public DbSet<AppendixTaspExpenditure> AppendixIVBTaspExpenditures { get; set; } = null!;
    public DbSet<AppendixEstablishmentExpenditure> AppendixVEstablishmentExpenditures { get; set; } = null!;
    public DbSet<AppendixGrantInAid> AppendixVAGrantInAids { get; set; } = null!;
    public DbSet<AppendixEstablishmentByObjectHead> AppendixVBEstablishmentByObjectHeads { get; set; } = null!;
    public DbSet<AppendixEstablishmentOtherThanAB> AppendixVCEstablishmentOtherThanABs { get; set; } = null!;
    public DbSet<AppendixNonTaxRevenue> AppendixVINonTaxRevenues { get; set; } = null!;
    public DbSet<AppendixUserCharges> AppendixVIAUserCharges { get; set; } = null!;
    public DbSet<AppendixPendingLiabilities> AppendixVIBPendingLiabilities { get; set; } = null!;
    public DbSet<AppendixCorpusFund> AppendixVICCorpusFunds { get; set; } = null!;
    public DbSet<AppendixInternalResources> AppendixVIDInternalResources { get; set; } = null!;
    public DbSet<AppendixCorpusFundAbGia> AppendixVIECorpusFundAbGias { get; set; } = null!;
    public DbSet<AppendixRecoveries> AppendixVIIARecoveries { get; set; } = null!;
    public DbSet<AppendixCommercialUndertakingReceipts> AppendixVIIBCommercialUndertakingReceipts { get; set; } = null!;
    public DbSet<AppendixLoansToGovtServants> AppendixXLoansToGovtServants { get; set; } = null!;
    public DbSet<PublicAccountReceiptPayment> PublicAccountReceiptPayments { get; set; } = null!;
    public DbSet<AppendixMinorHeadUserCharges> AppendixVIFMinorHeadUserCharges { get; set; } = null!;
    public DbSet<AppendixSchemeAppraisalStatus> AppendixIIIBSchemeAppraisalStatuses { get; set; } = null!;
    public DbSet<AppendixUserChargesAutonomousBody> AppendixVIGUserChargesAutonomousBodies { get; set; } = null!;

    // Read-only shared reference data (migrated from legacy BIMSDemo 2026-07-29) - drives Appendix
    // III's Balance Type -> Scheme -> SubScheme cascade.
    public DbSet<MCategory> Categories { get; set; } = null!;
    public DbSet<MScheme> Schemes { get; set; } = null!;
    public DbSet<MSubScheme> SubSchemes { get; set; } = null!;
    public DbSet<MObjectHead> ObjectHeads { get; set; } = null!;
    public DbSet<MDemand> Demands { get; set; } = null!;
    public DbSet<MDepartment> Departments { get; set; } = null!;
    public DbSet<SbeNbeSummary> SbeNbeSummaries { get; set; } = null!;
    public DbSet<SbeNbeSummaryByYear> SbeNbeSummariesByYear { get; set; } = null!;
    public DbSet<MMajorHead> MajorHeads { get; set; } = null!;
    public DbSet<MDemandMajorHead> DemandMajorHeads { get; set; } = null!;
    public DbSet<SbeData> SbeDataRows { get; set; } = null!;
    public DbSet<TQepData> QepDataRows { get; set; } = null!;
    public DbSet<DdgNew> DdgNewRows { get; set; } = null!;
    public DbSet<MSpecialScheme> SpecialSchemes { get; set; } = null!;
    public DbSet<AppendixViibTransactionType> AppendixViibTransactionTypes { get; set; } = null!;
    public DbSet<AppendixViReceiptType> AppendixViReceiptTypes { get; set; } = null!;
    public DbSet<MinorHeadName> MinorHeadNames { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AppendixConfiguration());
        modelBuilder.ApplyConfiguration(new AutonomousBodyConfiguration());
        modelBuilder.ApplyConfiguration(new AutonomousBodyRequestConfiguration());
        modelBuilder.ApplyConfiguration(new DemandAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new RemarkConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogEntryConfiguration());

        modelBuilder.ApplyConfiguration(new AppendixBudgetExpenditureTrendConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixProjectedDemandConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixQuarterlyExpenditurePlanConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixCnaSnaBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixTsaAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixEstimatesOfSchemesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixScspExpenditureConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixTaspExpenditureConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixEstablishmentExpenditureConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixGrantInAidConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixEstablishmentByObjectHeadConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixEstablishmentOtherThanABConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixNonTaxRevenueConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixUserChargesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixPendingLiabilitiesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixCorpusFundConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixInternalResourcesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixCorpusFundAbGiaConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixRecoveriesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixCommercialUndertakingReceiptsConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixLoansToGovtServantsConfiguration());
        modelBuilder.ApplyConfiguration(new PublicAccountReceiptPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixMinorHeadUserChargesConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixUserChargesAutonomousBodyConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixSchemeAppraisalStatusConfiguration());

        modelBuilder.ApplyConfiguration(new MCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new MSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new MSubSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new MObjectHeadConfiguration());
        modelBuilder.ApplyConfiguration(new MDemandConfiguration());
        modelBuilder.ApplyConfiguration(new MDepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new SbeNbeSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new SbeNbeSummaryByYearConfiguration());
        modelBuilder.ApplyConfiguration(new MMajorHeadConfiguration());
        modelBuilder.ApplyConfiguration(new MDemandMajorHeadConfiguration());
        modelBuilder.ApplyConfiguration(new SbeDataConfiguration());
        modelBuilder.ApplyConfiguration(new TQepDataConfiguration());
        modelBuilder.ApplyConfiguration(new DdgNewConfiguration());
        modelBuilder.ApplyConfiguration(new MSpecialSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixViibTransactionTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AppendixViReceiptTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MinorHeadNameConfiguration());
    }

    /// <summary>
    /// Populates PreBudgetAuditLog (decision 1 — generic audit table instead of 21 per-appendix
    /// _log tables) by snapshotting every tracked Added/Modified/Deleted entity's old/new values as
    /// JSON before delegating to the real save. Runs for every entity type, not just appendices —
    /// AuditLogEntry itself is excluded to avoid self-referential noise.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pending = BuildPendingAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count > 0)
        {
            // RecordId is re-resolved here, AFTER the real save - for a newly Added row, the PK
            // property only holds EF's temporary negative placeholder key until base.SaveChangesAsync
            // actually runs the INSERT and back-fills the real database-generated Id onto the tracked
            // entity (bug found 2026-08-14 via smoke test: every Insert audit row had RecordId set to
            // that placeholder instead of the real Id). Harmless to re-resolve for Update/Delete/
            // Freeze too, since their Id never changes across the save.
            foreach (var (auditEntry, trackedEntry) in pending)
            {
                auditEntry.RecordId = GetRecordId(trackedEntry) ?? auditEntry.RecordId;
            }

            AuditLog.AddRange(pending.Select(p => p.AuditEntry));
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private List<(AuditLogEntry AuditEntry, EntityEntry TrackedEntry)> BuildPendingAuditEntries()
    {
        var entries = new List<(AuditLogEntry, EntityEntry)>();
        var callerId = GetCallerUserId();
        var callerUserName = _httpContextAccessor?.HttpContext?.User?.FindFirst("UserName")?.Value;
        var callerIp = GetClientIp();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLogEntry || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
            {
                continue;
            }

            var recordId = GetRecordId(entry);
            if (recordId is null)
            {
                continue;
            }

            entries.Add((new AuditLogEntry
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                RecordId = recordId.Value,
                Action = ResolveAction(entry),
                ChangedByUserId = callerId,
                ChangedByUserName = callerUserName,
                ChangedByIp = callerIp,
                ChangedAtUtc = DateTime.UtcNow,
                OldValuesJson = entry.State == EntityState.Added ? null : JsonSerializer.Serialize(GetValues(entry, useOriginal: true)),
                NewValuesJson = entry.State == EntityState.Deleted ? null : JsonSerializer.Serialize(GetValues(entry, useOriginal: false))
            }, entry));
        }

        return entries;
    }

    /// <summary>
    /// Appendix entities never actually hit EF's own EntityState.Deleted - AppendixEntityBase.
    /// MarkDeleted just flips IsDeleted (soft delete), which EF sees as an ordinary Modified. This
    /// distinguishes that soft-delete (and row-level Freeze, same shape) from a genuine field-level
    /// Update by checking whether IsDeleted/IsFrozen actually flipped false-&gt;true this save, so
    /// PreBudgetAuditLog.Action reads "Delete"/"Freeze" rather than a generic "Update" for those.
    /// </summary>
    private static string ResolveAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditAction.Insert;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditAction.Delete;
        }

        if (entry.Entity is AppendixEntityBase appendixEntity)
        {
            var isDeletedProp = entry.Property(nameof(AppendixEntityBase.IsDeleted));
            if (appendixEntity.IsDeleted && isDeletedProp.OriginalValue is false)
            {
                return AuditAction.Delete;
            }

            var isFrozenProp = entry.Property(nameof(AppendixEntityBase.IsFrozen));
            if (appendixEntity.IsFrozen && isFrozenProp.OriginalValue is false)
            {
                return AuditAction.Freeze;
            }
        }

        return AuditAction.Update;
    }

    private int? GetCallerUserId()
    {
        var sub = _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>Same X-Forwarded-For-aware resolution used by AppendixDataRepository&lt;T&gt; and AIM's AuthenticationService.ExtractClientIpAddress - requests reach this service through the Gateway.</summary>
    private string? GetClientIp()
    {
        var context = _httpContextAccessor?.HttpContext;
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

    private static int? GetRecordId(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue is int id ? id : null;
    }

    private static Dictionary<string, object?> GetValues(EntityEntry entry, bool useOriginal)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            values[property.Metadata.Name] = useOriginal ? property.OriginalValue : property.CurrentValue;
        }

        return values;
    }
}
