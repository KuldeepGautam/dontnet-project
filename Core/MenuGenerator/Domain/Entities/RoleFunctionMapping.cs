namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy M_MapRoleFunction table (read-only, renamed 2026-08-04 from M_RoleFunctionMapping).
/// Grants a Role visibility of a Function within a Module (ModuleId here is the
/// mapping's own value, joined against RoleModuleMapping.ModuleId, per the
/// reference SQL this service was designed against).
/// </summary>
public class RoleFunctionMapping
{
    public int RfMappingId { get; set; }
    public int? AppId { get; set; }
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public int FunctionId { get; set; }
    public string Active { get; set; } = "N";
    public int RfSequenceNo { get; set; }

    /// <summary>"Y"/"N" — freezes this role/function mapping regardless of <see cref="Active"/>.</summary>
    public string RFFreez { get; set; } = "N";

    /// <summary>"Y"/"N" — legacy per-mapping user-facing freeze flag, distinct from <see cref="RFFreez"/>.</summary>
    public string UserRFFreez { get; set; } = "N";
}
