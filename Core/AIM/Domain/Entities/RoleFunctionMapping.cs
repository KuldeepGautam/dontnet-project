namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_MapRoleFunction</c> table (int identity, renamed
/// 2026-08-04 from <c>M_RoleFunctionMapping</c>) — the same physical table MenuGenerator already
/// reads read-only. Grants a Role existence-based access to
/// a Function; there are no CRUD (Create/Read/Update/Delete) columns in the real schema — the
/// earlier Guid-keyed <c>aim.RoleFunctionMapping</c>'s <c>CanCreate</c>/<c>CanRead</c>/
/// <c>CanUpdate</c>/<c>CanDelete</c> flags were invented, never grounded, and have been dropped.
/// A mapping row existing (and not frozen) means full access to that Function. Added 2026-07-13.
/// </summary>
public class RoleFunctionMapping
{
    public int RFMappingId { get; set; }

    public int? AppId { get; set; }

    public int RoleId { get; set; }

    public int? ModuleId { get; set; }

    public int FunctionId { get; set; }

    /// <summary>"Y"/"N" (deny-list convention: anything but "N" is active).</summary>
    public string Active { get; set; } = "Y";

    public int? RFSequenceNo { get; set; }

    public string? Remarks { get; set; }

    /// <summary>"Y"/"N" — freezes this mapping regardless of <see cref="Active"/>.</summary>
    public string RFFreez { get; set; } = "N";

    /// <summary>"Y"/"N" — legacy per-mapping user-facing freeze flag, distinct from <see cref="RFFreez"/>.</summary>
    public string UserRFFreez { get; set; } = "N";

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Function Function { get; set; } = null!;
}
