namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;

/// <summary>Outcome of migrating one legacy BIMSDemo table into its optimized destination table.</summary>
public record LegacyMigrationResult(string Appendix, int SourceRowCount, int AlreadyMigrated, int Inserted, int SkippedInvalid, string? Note = null);

/// <summary>
/// One-time, idempotent migration of legacy BIMSDemo appendix data (Temp_BudExpTrends,
/// Temp_ProjDemand, Temp_AppendixII, Temp_EstExp, Temp_GiA_AB, Temp_EstExp_OtherthanAB) into this
/// service's own optimized Appendix_* tables, using EF Core instead of the legacy stored
/// procedures. BIMSDemo lives on the same SQL Server instance as UBIS-Dev, so legacy rows are read
/// with Database.SqlQueryRaw against the fully-qualified cross-database table name — no linked
/// server or second connection needed.
///
/// Idempotency: every method loads the natural key(s) already present in the destination table
/// first (AsNoTracking, key columns only) and only inserts legacy rows whose key isn't already
/// there, so re-running a method after a partial or repeat run never creates duplicates.
///
/// Appendix III (Temp_IIICNASNA) and Appendix III-A (Temp_AppendixIIIA) already have their legacy
/// data migrated (confirmed 2026-07-31: destination row counts equal legacy row count + pre-existing
/// test rows) - not repeated here. Appendix IV, IV-A, IV-B and V-B already have dedicated SQL
/// migration scripts under Others\publish-staging - not repeated here either.
/// </summary>
public class LegacyAppendixDataMigrationService
{
    private readonly PreBudgetDbContext _db;
    private readonly ILogger<LegacyAppendixDataMigrationService> _logger;

    public LegacyAppendixDataMigrationService(PreBudgetDbContext db, ILogger<LegacyAppendixDataMigrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private record BudExpTrendRow(int? DemandID, string? FinYear, decimal? Rev_BE, decimal? Rev_RE, decimal? Rev_Actuals, decimal? Rev_ActualsSept, decimal? Cap_BE, decimal? Cap_RE, decimal? Cap_Actuals, decimal? Cap_ActualsSept);

    /// <summary>
    /// Appendix I - Budget and Expenditure Trends. Legacy sp_Insert_Appendix1_Data's 'Insert' branch
    /// always inserted exactly 5 rows per demand (years Y6..Y2), one row per (DemandID, FinYear) -
    /// FinYear (the row's own year), not the legacy table's redundant FinancialYear (filing-year
    /// context repeated on all 5 rows), maps to the destination FinancialYear column, matching how
    /// AppendixIViewModel already renders one grid row per year.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixIAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinYear, Rev_BE, Rev_RE, Rev_Actuals, Rev_ActualsSept, Cap_BE, Cap_RE, Cap_Actuals, Cap_ActualsSept
            FROM BIMSDemo.dbo.Temp_BudExpTrends
            """;

        var source = await _db.Database.SqlQueryRaw<BudExpTrendRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixIBudgetExpenditureTrends.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear))
            .ToHashSet();

        var toInsert = new List<AppendixBudgetExpenditureTrend>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinYear))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinYear);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixBudgetExpenditureTrend
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinYear,
                RevenueBE = row.Rev_BE,
                RevenueRE = row.Rev_RE,
                RevenueActuals = row.Rev_Actuals,
                RevenueActualsUptoSept = row.Rev_ActualsSept,
                CapitalBE = row.Cap_BE,
                CapitalRE = row.Cap_RE,
                CapitalActuals = row.Cap_Actuals,
                CapitalActualsUptoSept = row.Cap_ActualsSept
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("I", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix I legacy migration: {Result}", result);
        return result;
    }

    private record ProjDemandRow(int? DemandID, string? FinancialYear, decimal? PrevYrMinRevBE, decimal? PrevYrMinRevRE, decimal? PrevYrMinCapBE, decimal? PrevYrMinCapRE, decimal? CurrYrMinRevBE, decimal? CurrYrMinCapBE, decimal? CurrYrMinMTEFRevBE, decimal? CurrYrMinMTEFCapBE);

    /// <summary>
    /// Appendix I-A - Projected Demand by Ministry. sp_Temp_ProjDemand's CheckDuplicateData action
    /// checks "WHERE DemandID=@DemandID" only (no year filter) - the legacy business rule is one
    /// submission ever per demand, so the natural key here is DemandID alone (confirmed: legacy
    /// table has exactly one row per distinct DemandID, 866 rows / 866 distinct demands).
    /// PrevYrMinCapBE is stored as nvarchar(max) in the legacy table (a data-entry defect - every
    /// other amount column is decimal) so it's parsed with TRY_CONVERT, which yields NULL for any
    /// row where free text was entered instead of a number.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixIAAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, PrevYrMinRevBE, PrevYrMinRevRE,
                   TRY_CONVERT(decimal(18,2), PrevYrMinCapBE) AS PrevYrMinCapBE, PrevYrMinCapRE,
                   CurrYrMinRevBE, CurrYrMinCapBE, CurrYrMinMTEFRevBE, CurrYrMinMTEFCapBE
            FROM BIMSDemo.dbo.Temp_ProjDemand
            """;

        var source = await _db.Database.SqlQueryRaw<ProjDemandRow>(sql).ToListAsync(ct);

        var existingDemandIds = (await _db.AppendixIAProjectedDemands.AsNoTracking()
            .Select(e => e.DemandId)
            .ToListAsync(ct))
            .ToHashSet();

        var toInsert = new List<AppendixProjectedDemand>();
        var skippedInvalid = 0;
        var seen = new HashSet<int>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear))
            {
                skippedInvalid++;
                continue;
            }

            if (existingDemandIds.Contains(row.DemandID.Value) || !seen.Add(row.DemandID.Value))
            {
                continue;
            }

            toInsert.Add(new AppendixProjectedDemand
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                PrevYrMinRevenueBE = row.PrevYrMinRevBE,
                PrevYrMinRevenueRE = row.PrevYrMinRevRE,
                PrevYrMinCapitalBE = row.PrevYrMinCapBE,
                PrevYrMinCapitalRE = row.PrevYrMinCapRE,
                CurrYrMinRevenueBE = row.CurrYrMinRevBE,
                CurrYrMinCapitalBE = row.CurrYrMinCapBE,
                CurrYrMinMtefRevenueBE = row.CurrYrMinMTEFRevBE,
                CurrYrMinMtefCapitalBE = row.CurrYrMinMTEFCapBE
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("I-A", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix I-A legacy migration: {Result}", result);
        return result;
    }

    private record AppendixIIRow(int? DemandID, string? FinancialYear, decimal? QEP_Approved_Q1_Pre, decimal? Actuals_Q1_Pre, decimal? QEP_Approved_Q1, decimal? Actuals_Q1, string? Remarks_Q1, decimal? QEP_Approved_Q2_Pre, decimal? Actuals_Q2_Pre, decimal? QEP_Approved_Q2, decimal? Actuals_Q2, string? Remarks_Q2);

    /// <summary>
    /// Appendix II - Quarterly Expenditure Plan. sp_Temp_AppendixIIQep's DisplayData/CheckDuplicateData
    /// both key off DemandID+FinancialYear (both present on the legacy table itself). Q1/Q2
    /// HasDeviation and Q1/Q2MofApprovalDetails are FRS-mandated fields the legacy table never had -
    /// they default to false/null on migrated rows, same as any other new entry.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixIIAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, QEP_Approved_Q1_Pre, Actuals_Q1_Pre, QEP_Approved_Q1, Actuals_Q1, Remarks_Q1,
                   QEP_Approved_Q2_Pre, Actuals_Q2_Pre, QEP_Approved_Q2, Actuals_Q2, Remarks_Q2
            FROM BIMSDemo.dbo.Temp_AppendixII
            """;

        var source = await _db.Database.SqlQueryRaw<AppendixIIRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixIIQuarterlyExpenditurePlans.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear))
            .ToHashSet();

        var toInsert = new List<AppendixQuarterlyExpenditurePlan>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixQuarterlyExpenditurePlan
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                Q1ApprovedQepPrevYear = row.QEP_Approved_Q1_Pre,
                Q1ActualsPrevYear = row.Actuals_Q1_Pre,
                Q1ApprovedQep = row.QEP_Approved_Q1,
                Q1Actuals = row.Actuals_Q1,
                RemarksQ1 = row.Remarks_Q1,
                Q2ApprovedQepPrevYear = row.QEP_Approved_Q2_Pre,
                Q2ActualsPrevYear = row.Actuals_Q2_Pre,
                Q2ApprovedQep = row.QEP_Approved_Q2,
                Q2Actuals = row.Actuals_Q2,
                RemarksQ2 = row.Remarks_Q2
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("II", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix II legacy migration: {Result}", result);
        return result;
    }

    private record GiaRow(int? DemandID, string? FinancialYear, string? AB_Name,
        decimal? GiAGen_Actuals, decimal? GiAGen_ActualsuptoSeptPrev, decimal? GiAGen_BE, decimal? GiAGen_ActualsuptoSept, decimal? GiAGen_RE, decimal? GiAGen_NBE,
        decimal? GiACCA_Actuals, decimal? GiACCA_ActualsuptoSeptPrev, decimal? GiACCA_BE, decimal? GiACCA_ActualsuptoSept, decimal? GiACCA_RE, decimal? GiACCA_NBE,
        decimal? GiASalary_Actuals, decimal? GiASalary_ActualsuptoSeptPrev, decimal? GiASalary_Total, decimal? GiASalary_BE, decimal? GiASalary_ActualsuptoSept, decimal? GiASalary_RE, decimal? GiASalary_NBE);

    /// <summary>
    /// Appendix V-A - Grant in Aid to Autonomous Bodies. Legacy Temp_GiA_AB.AB_Name is free text (not
    /// an FK) but the current AppendixGrantInAid entity requires a NOT NULL AutonomousBodyId FK into
    /// this service's own M_AutonomousBody per FR-004 - so only legacy rows whose AB_Name matches an
    /// M_AutonomousBody.Name exactly (trimmed, case-insensitive) can be migrated. Confirmed against
    /// the live data: 565 of 3,446 legacy rows (16%) match; the remaining rows reference
    /// autonomous-body names that exist nowhere in the current master list (a legacy data-quality
    /// gap, not a migration bug - see SkippedInvalid count / Note on the returned result).
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixVAAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, AB_Name,
                   GiAGen_Actuals, GiAGen_ActualsuptoSeptPrev, GiAGen_BE, GiAGen_ActualsuptoSept, GiAGen_RE, GiAGen_NBE,
                   GiACCA_Actuals, GiACCA_ActualsuptoSeptPrev, GiACCA_BE, GiACCA_ActualsuptoSept, GiACCA_RE, GiACCA_NBE,
                   GiASalary_Actuals, GiASalary_ActualsuptoSeptPrev, GiASalary_Total, GiASalary_BE, GiASalary_ActualsuptoSept, GiASalary_RE, GiASalary_NBE
            FROM BIMSDemo.dbo.Temp_GiA_AB
            """;

        var source = await _db.Database.SqlQueryRaw<GiaRow>(sql).ToListAsync(ct);

        // GroupBy + first-wins instead of ToDictionaryAsync: M_AutonomousBody has a handful of
        // case-insensitive duplicate names (e.g. "Maulana Azad Education Foundation" appears twice
        // with different ids), so a straight ToDictionary throws on the second occurrence.
        var autonomousBodies = await _db.AutonomousBodies.AsNoTracking()
            .Select(a => new { a.AutonomousBodyId, a.Name })
            .ToListAsync(ct);
        var autonomousBodyIdByName = autonomousBodies
            .GroupBy(a => a.Name.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First().AutonomousBodyId);

        var existingKeys = (await _db.AppendixVAGrantInAids.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.AutonomousBodyId })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.AutonomousBodyId))
            .ToHashSet();

        var toInsert = new List<AppendixGrantInAid>();
        var skippedInvalid = 0;
        var alreadyMigrated = 0;
        var seen = new HashSet<(int, string, int)>();
        var unmatchedNames = new HashSet<string>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear))
            {
                skippedInvalid++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.AB_Name) || !autonomousBodyIdByName.TryGetValue(row.AB_Name.Trim().ToUpperInvariant(), out var autonomousBodyId))
            {
                skippedInvalid++;
                if (!string.IsNullOrWhiteSpace(row.AB_Name))
                {
                    unmatchedNames.Add(row.AB_Name.Trim());
                }
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, autonomousBodyId);
            if (existingKeys.Contains(key))
            {
                alreadyMigrated++;
                continue;
            }

            if (!seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixGrantInAid
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                AutonomousBodyId = autonomousBodyId,
                GiaGeneralActuals = row.GiAGen_Actuals,
                GiaGeneralActualsUptoSeptPrevYear = row.GiAGen_ActualsuptoSeptPrev,
                GiaGeneralBE = row.GiAGen_BE,
                GiaGeneralActualsUptoSept = row.GiAGen_ActualsuptoSept,
                GiaGeneralRE = row.GiAGen_RE,
                GiaGeneralNBE = row.GiAGen_NBE,
                GiaCcaActuals = row.GiACCA_Actuals,
                GiaCcaActualsUptoSeptPrevYear = row.GiACCA_ActualsuptoSeptPrev,
                GiaCcaBE = row.GiACCA_BE,
                GiaCcaActualsUptoSept = row.GiACCA_ActualsuptoSept,
                GiaCcaRE = row.GiACCA_RE,
                GiaCcaNBE = row.GiACCA_NBE,
                GiaSalaryActuals = row.GiASalary_Actuals,
                GiaSalaryActualsUptoSeptPrevYear = row.GiASalary_ActualsuptoSeptPrev,
                GiaSalaryTotal = row.GiASalary_Total,
                GiaSalaryBE = row.GiASalary_BE,
                GiaSalaryActualsUptoSept = row.GiASalary_ActualsuptoSept,
                GiaSalaryRE = row.GiASalary_RE,
                GiaSalaryNBE = row.GiASalary_NBE
            });
        }

        await InsertBatchAsync(toInsert, ct);

        if (unmatchedNames.Count > 0)
        {
            _logger.LogWarning(
                "Appendix V-A legacy migration: {Count} distinct AB_Name values had no matching M_AutonomousBody row (sample: {Sample})",
                unmatchedNames.Count, string.Join("; ", unmatchedNames.Take(10)));
        }

        var result = new LegacyMigrationResult("V-A", source.Count, alreadyMigrated, toInsert.Count, skippedInvalid,
            unmatchedNames.Count > 0 ? $"{unmatchedNames.Count} distinct legacy AB_Name values do not match any M_AutonomousBody.Name and were skipped." : null);
        _logger.LogInformation("Appendix V-A legacy migration: {Result}", result);
        return result;
    }

    private record EstExpRow(int? DemandID, string? FinancialYear, string? Name, decimal? Actuals, decimal? ActualsuptoSeptPrev, decimal? BE, decimal? ActualsuptoSept, decimal? Proposed_RE, decimal? Budget_RE, decimal? Proposed_NBE, decimal? Budget_NBE, string? Remarks_Budget);

    /// <summary>
    /// Appendix V - Estimates of Establishment &amp; Other Central Expenditure. sp_Insert_Temp_EstExp
    /// always wrote exactly 6 rows per demand (Salary/Non-Salary/GiA General/GiA CCA/GiA Salary/Other
    /// than AB - see EstablishmentExpenditureCategory), one call rejected outright if any row for
    /// that DemandID already existed ("Recommended amount has already been submitted by Budget
    /// Division") - so the natural key is (DemandId, FinancialYear, Category), same as the other
    /// free-text-discriminator appendices (V-C).
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixVAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, Name, Actuals, ActualsuptoSeptPrev, BE, ActualsuptoSept, Proposed_RE, Budget_RE, Proposed_NBE, Budget_NBE, Remarks_Budget
            FROM BIMSDemo.dbo.Temp_EstExp
            """;

        var source = await _db.Database.SqlQueryRaw<EstExpRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixVEstablishmentExpenditures.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.Category })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.Category))
            .ToHashSet();

        var toInsert = new List<AppendixEstablishmentExpenditure>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, string)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear) || string.IsNullOrWhiteSpace(row.Name)
                || !EstablishmentExpenditureCategory.All.Contains(row.Name))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.Name);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixEstablishmentExpenditure
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                Category = row.Name,
                Actuals = row.Actuals,
                ActualsUptoSeptPrevYear = row.ActualsuptoSeptPrev,
                BE = row.BE,
                ActualsUptoSept = row.ActualsuptoSept,
                ProposedRE = row.Proposed_RE,
                BudgetRecommendedRE = row.Budget_RE,
                ProposedNBE = row.Proposed_NBE,
                BudgetRecommendedNBE = row.Budget_NBE,
                RemarksBudget = row.Remarks_Budget
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("V", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix V legacy migration: {Result}", result);
        return result;
    }

    private record EstExpOtherThanAbRow(int? DemandID, string? FinancialYear, string? Name, decimal? Actuals, decimal? ActualsuptoSeptPrev, decimal? BE, decimal? ActualsuptoSept, decimal? Proposed_RE, decimal? Proposed_NBE, string? Remarks);

    /// <summary>
    /// Appendix V-C - Establishment Expenditure Other Than Autonomous Bodies. sp_Select/Insert/
    /// Update/Delete_Temp_EstExp_OtherThanAB key off RowId for update/delete and DemandID (+implicit
    /// FinancialYear) for select; Name is free text on both the legacy table and the current entity,
    /// so no lookup/FK resolution is needed here (unlike V-A).
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixVCAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, Name, Actuals, ActualsuptoSeptPrev, BE, ActualsuptoSept, Proposed_RE, Proposed_NBE, Remarks
            FROM BIMSDemo.dbo.Temp_EstExp_OtherthanAB
            """;

        var source = await _db.Database.SqlQueryRaw<EstExpOtherThanAbRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixVCEstablishmentOtherThanABs.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.Name })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.Name))
            .ToHashSet();

        var toInsert = new List<AppendixEstablishmentOtherThanAB>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, string)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear) || string.IsNullOrWhiteSpace(row.Name))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.Name);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixEstablishmentOtherThanAB
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                Name = row.Name,
                Actuals = row.Actuals,
                ActualsUptoSeptPrevYear = row.ActualsuptoSeptPrev,
                BE = row.BE,
                ActualsUptoSept = row.ActualsuptoSept,
                ProposedRE = row.Proposed_RE,
                ProposedNBE = row.Proposed_NBE,
                Remarks = row.Remarks
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("V-C", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix V-C legacy migration: {Result}", result);
        return result;
    }

    private record RecoveriesRow(int? DemandID, string? FinancialYear, string? SchemeName, int? MajorHeadId, string? MajorHeadCode, decimal? Actuals, decimal? BE, decimal? RE, decimal? NBE);

    /// <summary>
    /// Appendix VII-A - Recoveries taken in reduction of expenditure. sp_Temp_VIIA_Recoveries'
    /// CheckDuplicateSchemeData action keys off (DemandID, SchemeName, MajorHead); FinancialYear is
    /// added here too since the destination table (and every other appendix) is scoped per financial
    /// year. Temp_Recoveries.Charged_Voted is NULL for every legacy row (confirmed against live data)
    /// so IsCharged always migrates as false, same as any other new entry. Temp_Recoveries.MajorHead
    /// is a 4-char legacy code, not the AppendixRecoveries.MajorHeadId (an external ReferenceData id)
    /// - resolved here via BIMSDemo.dbo.M_MajorHead.MajorHeadCode, which is the same master table the
    /// ReferenceData service's own major-head list originates from, so its MajorHeadId values line up
    /// with what ReferenceData will report at runtime. Rows whose code has no match are skipped and
    /// reported (never silently dropped) via SkippedInvalid/Note.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixVIIAAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT t.DemandID, t.FinancialYear, t.SchemeName, mh.MajorHeadId AS MajorHeadId, t.MajorHead AS MajorHeadCode, t.Actuals, t.BE, t.RE, t.NBE
            FROM BIMSDemo.dbo.Temp_Recoveries t
            LEFT JOIN BIMSDemo.dbo.M_MajorHead mh ON mh.MajorHeadCode = t.MajorHead
            """;

        var source = await _db.Database.SqlQueryRaw<RecoveriesRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixVIIARecoveries.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.MajorHeadId, e.SchemeName })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.MajorHeadId, k.SchemeName))
            .ToHashSet();

        var toInsert = new List<AppendixRecoveries>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, int?, string?)>();
        var unmatchedCodes = new HashSet<string>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear) || string.IsNullOrWhiteSpace(row.SchemeName))
            {
                skippedInvalid++;
                continue;
            }

            if (row.MajorHeadId is null)
            {
                skippedInvalid++;
                if (!string.IsNullOrWhiteSpace(row.MajorHeadCode))
                {
                    unmatchedCodes.Add(row.MajorHeadCode.Trim());
                }
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.MajorHeadId, row.SchemeName);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixRecoveries
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                SchemeName = row.SchemeName,
                MajorHeadId = row.MajorHeadId,
                IsCharged = false,
                Actuals = row.Actuals,
                BE = row.BE,
                RE = row.RE,
                NBE = row.NBE
            });
        }

        await InsertBatchAsync(toInsert, ct);

        if (unmatchedCodes.Count > 0)
        {
            _logger.LogWarning(
                "Appendix VII-A legacy migration: {Count} distinct MajorHead codes had no matching M_MajorHead row (sample: {Sample})",
                unmatchedCodes.Count, string.Join("; ", unmatchedCodes.Take(10)));
        }

        var result = new LegacyMigrationResult("VII-A", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid,
            unmatchedCodes.Count > 0 ? $"{unmatchedCodes.Count} distinct legacy MajorHead codes do not match any M_MajorHead.MajorHeadCode and were skipped." : null);
        _logger.LogInformation("Appendix VII-A legacy migration: {Result}", result);
        return result;
    }

    private record CommUnderRow(int? DemandID, string? FinancialYear, int? SchemeId, string? TransactionType, int? MajorHeadId, string? MajorHeadCode,
        decimal? ActualsY2, decimal? ActualsY1, decimal? ActualsUptoSept, decimal? ActualsUptoSeptPrevYear, decimal? BE, decimal? RE, decimal? IncreasedBE, decimal? NBE);

    /// <summary>
    /// Appendix VII-B - Commercial receipts of departmentally-run commercial undertakings.
    /// sp_Temp_Appendix7b keys duplicate-checking off (DemandID, Spl_SchemeId, TransactionType,
    /// MajorHead); FinancialYear is added for the same per-year-scoping reason as VII-A.
    /// Spl_SchemeId already matches AppendixCommercialUndertakingReceipts.SchemeId 1:1 (both are the
    /// same M_SpecialScheme id space) so it's passed through unresolved. MajorHead is the same 4-char
    /// legacy code as VII-A and is resolved the same way via BIMSDemo.dbo.M_MajorHead.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixVIIBAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT t.DemandID, t.FinancialYear, t.Spl_SchemeId AS SchemeId, t.TransactionType, mh.MajorHeadId AS MajorHeadId, t.MajorHead AS MajorHeadCode,
                   t.Actuals_F_2 AS ActualsY2, t.Actuals_F_1 AS ActualsY1, t.Actual_Upto_Sept AS ActualsUptoSept, t.Actual_Upto_Sept_Prev AS ActualsUptoSeptPrevYear,
                   t.BE, t.RE, t.Increased_BE AS IncreasedBE, t.NBE
            FROM BIMSDemo.dbo.Temp_VIIB_CommUnder t
            LEFT JOIN BIMSDemo.dbo.M_MajorHead mh ON mh.MajorHeadCode = t.MajorHead
            """;

        var source = await _db.Database.SqlQueryRaw<CommUnderRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixVIIBCommercialUndertakingReceipts.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.SchemeId, e.TransactionType, e.MajorHeadId })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.SchemeId, k.TransactionType, k.MajorHeadId))
            .ToHashSet();

        var toInsert = new List<AppendixCommercialUndertakingReceipts>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, int?, string?, int?)>();
        var unmatchedCodes = new HashSet<string>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear))
            {
                skippedInvalid++;
                continue;
            }

            if (row.MajorHeadId is null)
            {
                skippedInvalid++;
                if (!string.IsNullOrWhiteSpace(row.MajorHeadCode))
                {
                    unmatchedCodes.Add(row.MajorHeadCode.Trim());
                }
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.SchemeId, row.TransactionType, row.MajorHeadId);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixCommercialUndertakingReceipts
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                SchemeId = row.SchemeId,
                TransactionType = row.TransactionType,
                MajorHeadId = row.MajorHeadId,
                ActualsY2 = row.ActualsY2,
                ActualsY1 = row.ActualsY1,
                ActualsUptoSept = row.ActualsUptoSept,
                ActualsUptoSeptPrevYear = row.ActualsUptoSeptPrevYear,
                BE = row.BE,
                RE = row.RE,
                IncreasedBE = row.IncreasedBE,
                NBE = row.NBE
            });
        }

        await InsertBatchAsync(toInsert, ct);

        if (unmatchedCodes.Count > 0)
        {
            _logger.LogWarning(
                "Appendix VII-B legacy migration: {Count} distinct MajorHead codes had no matching M_MajorHead row (sample: {Sample})",
                unmatchedCodes.Count, string.Join("; ", unmatchedCodes.Take(10)));
        }

        var result = new LegacyMigrationResult("VII-B", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid,
            unmatchedCodes.Count > 0 ? $"{unmatchedCodes.Count} distinct legacy MajorHead codes do not match any M_MajorHead.MajorHeadCode and were skipped." : null);
        _logger.LogInformation("Appendix VII-B legacy migration: {Result}", result);
        return result;
    }

    private record LoansRow(int? DemandID, string? FinancialYear, string? SubHeadName, decimal? ActualsY1, decimal? ActualsY2, decimal? ActualsY3, decimal? ActualsUptoSept, decimal? BE, decimal? RE, decimal? NBE);

    /// <summary>
    /// Appendix X - Loan to Government Servants, etc. Temp_Loans_GovtServants is already flat
    /// (one row per SubHeadName per DemandID/FinancialYear) - sp_Insert_Appendix12's wide-to-tall
    /// pivot and its non-obvious BERev/RECap-style parameter-to-column mapping only affect how rows
    /// were originally written, not how they're read back here, so columns map straight across.
    /// SubHeadName carries its own roman-numeral prefix baked into the text (e.g. "(iii) Advances for
    /// purchase of other motors conveyances" vs "(iv) Advances for purchase of other motors
    /// conveyances"); despite the near-identical descriptive suffix, the full strings differ by
    /// numeral and are NOT a data collision (confirmed: distinct SubHeadName values, ~244 rows each) -
    /// so (DemandId, FinancialYear, SubHeadName) is a safe natural key with no extra disambiguation.
    /// </summary>
    public async Task<LegacyMigrationResult> MigrateAppendixXAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, SubHeadName, Actuals1 AS ActualsY1, Actuals2 AS ActualsY2, Actuals3 AS ActualsY3, Actuals_uptoSept AS ActualsUptoSept, BE, RE, NBE
            FROM BIMSDemo.dbo.Temp_Loans_GovtServants
            """;

        var source = await _db.Database.SqlQueryRaw<LoansRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.AppendixXLoansToGovtServants.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.SubHeadName })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.SubHeadName))
            .ToHashSet();

        var toInsert = new List<AppendixLoansToGovtServants>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, string?)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear) || string.IsNullOrWhiteSpace(row.SubHeadName))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.SubHeadName);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new AppendixLoansToGovtServants
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                SubHeadName = row.SubHeadName,
                ActualsY1 = row.ActualsY1,
                ActualsY2 = row.ActualsY2,
                ActualsY3 = row.ActualsY3,
                ActualsUptoSept = row.ActualsUptoSept,
                BE = row.BE,
                RE = row.RE,
                NBE = row.NBE
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("X", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix X legacy migration: {Result}", result);
        return result;
    }

    private record PaRecPayRow(int? DemandID, string? FinancialYear, int? MajorHeadId,
        decimal? ActualReceipt, decimal? ActualPayment, decimal? BalanceAtEndReceipt, decimal? BalanceAtEndPayment,
        decimal? BEReceipt, decimal? BEPayment, decimal? AdjustmentReceipt, decimal? AdjustmentPayment,
        decimal? REReceipt, decimal? REPayment, decimal? NBEReceipt, decimal? NBEPayment, string? RemarksReceipt, string? RemarksPayment);

    /// <summary>
    /// Appendix PA - Public Account Receipt/Payment (Article 266(2)). Temp_PA_RecPay.MajorHeadID is
    /// already a surrogate int FK into BIMSDemo.dbo.M_MajorHead (unlike VII-A/VII-B's char(4) code),
    /// and that's the same id space PublicAccountReceiptPayment.MajorHeadId (an external ReferenceData
    /// id) expects, so it's passed through unresolved, matching the entity's own doc comment that its
    /// field list is provisional (FRS Open Item OI-05). Template_ID exists on the legacy table but has
    /// no equivalent field on the current entity, so it's not migrated. Natural key is
    /// (DemandId, FinancialYear, MajorHeadId) per sp_Temp_PA_RecPay's DisplayData filter shape.
    /// </summary>
    public async Task<LegacyMigrationResult> MigratePublicAccountReceiptPaymentAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DemandID, FinancialYear, MajorHeadID AS MajorHeadId,
                   Actual_Receipt AS ActualReceipt, Actual_Payment AS ActualPayment,
                   Bal_AtTheEnd_Receipt AS BalanceAtEndReceipt, Bal_AtTheEndPayment AS BalanceAtEndPayment,
                   BE_Receipt AS BEReceipt, BE_Payment AS BEPayment,
                   Adjustment_Receipt AS AdjustmentReceipt, Adjustment_Payment AS AdjustmentPayment,
                   Re_Receipt AS REReceipt, RE_Payment AS REPayment,
                   NBE_Receipt AS NBEReceipt, NBE_Payment AS NBEPayment,
                   Remark_Receipt AS RemarksReceipt, Remark_Payment AS RemarksPayment
            FROM BIMSDemo.dbo.Temp_PA_RecPay
            """;

        var source = await _db.Database.SqlQueryRaw<PaRecPayRow>(sql).ToListAsync(ct);

        var existingKeys = (await _db.PublicAccountReceiptPayments.AsNoTracking()
            .Select(e => new { e.DemandId, e.FinancialYear, e.MajorHeadId })
            .ToListAsync(ct))
            .Select(k => (k.DemandId, k.FinancialYear, k.MajorHeadId))
            .ToHashSet();

        var toInsert = new List<PublicAccountReceiptPayment>();
        var skippedInvalid = 0;
        var seen = new HashSet<(int, string, int?)>();

        foreach (var row in source)
        {
            if (row.DemandID is null || string.IsNullOrWhiteSpace(row.FinancialYear))
            {
                skippedInvalid++;
                continue;
            }

            var key = (row.DemandID.Value, row.FinancialYear, row.MajorHeadId);
            if (existingKeys.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            toInsert.Add(new PublicAccountReceiptPayment
            {
                DemandId = row.DemandID.Value,
                FinancialYear = row.FinancialYear,
                MajorHeadId = row.MajorHeadId,
                ActualReceipt = row.ActualReceipt,
                ActualPayment = row.ActualPayment,
                BalanceAtEndReceipt = row.BalanceAtEndReceipt,
                BalanceAtEndPayment = row.BalanceAtEndPayment,
                BEReceipt = row.BEReceipt,
                BEPayment = row.BEPayment,
                AdjustmentReceipt = row.AdjustmentReceipt,
                AdjustmentPayment = row.AdjustmentPayment,
                REReceipt = row.REReceipt,
                REPayment = row.REPayment,
                NBEReceipt = row.NBEReceipt,
                NBEPayment = row.NBEPayment,
                RemarksReceipt = row.RemarksReceipt,
                RemarksPayment = row.RemarksPayment
            });
        }

        await InsertBatchAsync(toInsert, ct);

        var result = new LegacyMigrationResult("PA-ReceiptPayment", source.Count, source.Count - toInsert.Count - skippedInvalid, toInsert.Count, skippedInvalid);
        _logger.LogInformation("Appendix PA-ReceiptPayment legacy migration: {Result}", result);
        return result;
    }

    /// <summary>
    /// Runs every migration in this class, each in its own transaction, and keeps going even if one
    /// appendix fails - a schema surprise in one legacy table should not block the others. Every
    /// failure is logged with the exception (never swallowed silently) and surfaced in the returned
    /// list as a zero-row result carrying the error message in Note.
    /// </summary>
    public async Task<List<LegacyMigrationResult>> MigrateAllAsync(CancellationToken ct)
    {
        var results = new List<LegacyMigrationResult>();
        var steps = new (string Name, Func<CancellationToken, Task<LegacyMigrationResult>> Run)[]
        {
            ("I", MigrateAppendixIAsync),
            ("I-A", MigrateAppendixIAAsync),
            ("II", MigrateAppendixIIAsync),
            ("V", MigrateAppendixVAsync),
            ("V-A", MigrateAppendixVAAsync),
            ("V-C", MigrateAppendixVCAsync),
            ("VII-A", MigrateAppendixVIIAAsync),
            ("VII-B", MigrateAppendixVIIBAsync),
            ("X", MigrateAppendixXAsync),
            ("PA-ReceiptPayment", MigratePublicAccountReceiptPaymentAsync)
        };

        foreach (var step in steps)
        {
            try
            {
                results.Add(await step.Run(ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy migration for Appendix {Appendix} failed", step.Name);
                results.Add(new LegacyMigrationResult(step.Name, 0, 0, 0, 0, $"FAILED: {ex.Message}"));
            }
        }

        return results;
    }

    private async Task InsertBatchAsync<TEntity>(List<TEntity> entities, CancellationToken ct) where TEntity : class
    {
        if (entities.Count == 0)
        {
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Set<TEntity>().AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
