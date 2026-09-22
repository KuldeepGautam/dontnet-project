namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_MapUserFY</c> table (int identity) — added 2026-08-03,
/// renamed from M_UserFY to M_MapUserFY 2026-08-04 to match this codebase's other M_MapUser*
/// junction-table naming (M_MapUserApp, M_MapUserRole), as part
/// of consolidating <c>dbo.M_User</c> from "one row per person per financial year" down to one row
/// per person. Holds what <c>User.FinancialYear</c> used to represent: one row per
/// (<see cref="UserId"/>, <see cref="FinancialYear"/>) pair a person is eligible to log in under.
/// <see cref="UserId"/> FKs to the single consolidated <see cref="User.UserId"/> row for that
/// person (same precedent as <see cref="MapUserRole.UserId"/> and <see cref="MapUserApp.UserId"/>).
/// Populated from every original (LoginId, FinancialYear) pair that existed in the pre-consolidation
/// <c>M_User</c> table — see <c>Others/publish-staging/migrate-consolidate-user-fy.sql</c> and
/// <c>dbo.Legacy_UserConsolidationMap</c> for the migration that built this table.
/// </summary>
public class UserFinancialYear
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
