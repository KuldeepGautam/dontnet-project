namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Central Financial Year master (dbo.M_FinancialYear, added 2026-07-30 per client suggestion) —
/// every module across UBIS_Web resolves/displays Financial Year from this table now, replacing
/// the old ad-hoc DefaultFinancialYear config / distinct-M_Users.FinancialYear fallback the Login
/// page used to seed its (until now invisible) default year with.
///
/// <see cref="AppId"/> added 2026-09-08 (client instruction: "M_FinancialYear should have AppId
/// ... each application like PreBudget, ECL, SBE can have different current financial years").
/// An earlier idea to identify rows with a synthetic per-app code (e.g. "PBCFY") was rejected
/// mid-design: a code that means "current" can't double as a stable historical identifier, since
/// "current" moves to a new row every year while anything that keys off the old row must never
/// have that meaning silently change. Settled shape instead: <see cref="AppId"/> (FK to
/// dbo.M_AppName - the same App master MenuGenerator already routes off, not a new naming scheme)
/// plus the existing plain <see cref="IsCurrentYear"/> bit, which moves between rows exactly as it
/// always has - nothing that references a specific (AppId, YearRange) row ever has to change when
/// the flag moves. Nullable for now - the original 6 pre-2026-09-08 rows are left in place with
/// AppId NULL (harmless/orphaned, superseded by the per-app rows seeded alongside this column) per
/// this repo's additive-only convention; every reader must now pass an explicit AppId.
/// </summary>
public class FinancialYear
{
    public int Id { get; set; }
    public string YearRange { get; set; } = string.Empty;
    public string BudgetType { get; set; } = string.Empty;
    public bool IsCurrentYear { get; set; }

    /// <summary>One-year-per-cycle counter, added 2026-08-04 — 2026-2027 is Budget Cycle 16, so
    /// e.g. 2025-2026 is 15, 2021-2022 is 11.</summary>
    public int BudgetCycle { get; set; }

    /// <summary>FK to dbo.M_AppName.AppId - which application this row's current-year flag
    /// belongs to (added 2026-09-08). Nullable only for the handful of legacy pre-rename rows;
    /// every row seeded going forward has one.</summary>
    public int? AppId { get; set; }
}
