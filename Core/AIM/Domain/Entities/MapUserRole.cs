namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_MapUserRole</c> table (int identity, renamed 2026-08-04
/// from <c>M_MapUserDemandFY</c>, itself renamed 2026-07-13 from <c>M_UserCharges</c>) —
/// authoritative source of a user's Role assignment (supersedes reading <see cref="User.Role"/>
/// directly, per user-confirmed decision). Consolidated 2026-08-04 from "one row per (UserId,
/// FinancialYear)" down to one row per user: data analysis found RoleId never varied by
/// FinancialYear for any user (0 counter-examples across 3797 pre-consolidation rows / 561 users),
/// and the app code never filtered by FinancialYear here either - it always resolved role by
/// UserId alone. The dropped FinancialYear/DemandCode columns' full history is preserved in
/// <c>dbo.Legacy_MapUserDemandFYMap</c> for forensic reference. <see cref="UserDemandMapping"/>
/// (dbo.M_MapUserDemand) remains the separate, unrelated source for which Demands a user can
/// access - this table is Role only.
/// </summary>
public class MapUserRole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    /// <summary>FK to <c>dbo.M_Role.RoleId</c> — this user's role.</summary>
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
