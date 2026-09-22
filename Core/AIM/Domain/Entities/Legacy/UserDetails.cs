namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read/write pass-through to the pre-existing legacy <c>dbo.UserDetails</c> table — a
/// User-to-Demand many-to-many mapping ("Profile Access" in the compliance brief: which
/// operational Demands a given legacy user is granted structural privileges over).
/// <see cref="UserId"/> is the same int identity as <see cref="User.UserId"/> directly (no
/// bridge needed since 2026-07-13's identity-model rewrite). Added 2026-07-10.
/// </summary>
public class UserDetails
{
    public int RowId { get; set; }

    public int UserId { get; set; }

    public int DemandId { get; set; }

    /// <summary>Prior row this one superseded, when a Demand allocation was revised. Null for the current/original row.</summary>
    public int? PrevRowId { get; set; }
}
