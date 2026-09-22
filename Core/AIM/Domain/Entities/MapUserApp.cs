namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_MapUserApp</c> table (int identity) — added 2026-08-03 to
/// replace the old comma-separated <c>M_User.AppId</c> column (e.g. "1,2,3,4,5") with a proper
/// junction table, one row per (<see cref="UserId"/>, <see cref="AppId"/>) pair. Confirmed before
/// the cutover that the old column was never read/parsed anywhere for an authorization decision —
/// safe to fully normalize and drop, unlike <see cref="UserDemandMapping.DemandIds"/> which is
/// still a live CSV column. <see cref="UserId"/> FKs directly to the specific per-year
/// <see cref="User.UserId"/> row the AppId list was read off of, same precedent as
/// <see cref="MapUserRole.UserId"/>.
/// </summary>
public class MapUserApp
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AppId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
