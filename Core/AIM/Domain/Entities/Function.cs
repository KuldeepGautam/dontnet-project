namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_Function</c> table (int identity) — the same physical
/// table MenuGenerator already reads read-only. Replaces the earlier Guid-keyed
/// <c>aim.Functions</c>; note there is no "FunctionCode" string column in the real schema —
/// authorization claims are keyed by <see cref="FunctionId"/> directly (see
/// <c>AuthenticationService.AuthenticateAsync</c>). Confirmed against the DBA's
/// <c>new-tables/M_Function.txt</c> export 2026-07-13: the real table has no "Active" column
/// either (only <see cref="Freez"/>) — access is governed entirely by
/// <see cref="RoleFunctionMapping.Active"/>/<see cref="RoleFunctionMapping.RFFreez"/>, not
/// anything on Function itself.
/// </summary>
public class Function
{
    public int FunctionId { get; set; }

    public int? ModuleId { get; set; }

    public string FunctionName { get; set; } = string.Empty;

    /// <summary>Legacy ASP.NET WebForms page path (e.g. "~/UBIS/Admin/RoleMaster.aspx") — not a
    /// usable REST route, kept only for reference/migration traceability.</summary>
    public string? AddressOfTheFunction { get; set; }

    public string? Remarks { get; set; }

    /// <summary>"Y"/"N" — freezes this function outright, independent of any role mapping.</summary>
    public string Freez { get; set; } = "N";

    // Navigation properties
    public ICollection<RoleFunctionMapping> RoleFunctionMappings { get; set; } = new List<RoleFunctionMapping>();
}
