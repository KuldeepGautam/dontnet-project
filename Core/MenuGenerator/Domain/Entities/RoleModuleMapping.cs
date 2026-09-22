namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy M_MapRoleModule table (read-only, renamed 2026-08-04 from M_RoleModuleMapping).
/// Grants a Role visibility of a Module within a given Application (AppId).
/// </summary>
public class RoleModuleMapping
{
    public int RModuleMappingId { get; set; }
    public int AppId { get; set; }
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public string Active { get; set; } = "N";
    public int RmSequenceNo { get; set; }

    /// <summary>"Y"/"N" — freezes this role/module mapping regardless of <see cref="Active"/>.</summary>
    public string RMFreez { get; set; } = "N";

    /// <summary>"Y"/"N" — legacy per-mapping user-facing freeze flag, distinct from <see cref="RMFreez"/>.</summary>
    public string UserMFreez { get; set; } = "N";
}
