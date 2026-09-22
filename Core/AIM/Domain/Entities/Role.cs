namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_Role</c> table (int identity) — the same physical table
/// MenuGenerator already reads read-only. Replaces the earlier Guid-keyed <c>aim.Roles</c>.
/// Added 2026-07-13.
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    /// <summary>"Y"/"N" (deny-list convention: anything but "N" is active).</summary>
    public string Active { get; set; } = "Y";

    // Navigation properties
    public ICollection<RoleFunctionMapping> RoleFunctionMappings { get; set; } = new List<RoleFunctionMapping>();
}
