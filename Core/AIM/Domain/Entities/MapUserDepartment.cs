namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the new <c>dbo.M_MapUserDepartment</c> table (int identity) — extracted from
/// <c>M_User.DepartmentId</c> 2026-08-17, since department assignment is a grant relationship, not
/// a core identity attribute (same reasoning as <see cref="UserDemandMapping"/>/<see cref="MapUserRole"/>).
/// Confirmed zero in-app writes to the old column before this extraction - reads only.
/// </summary>
public class MapUserDepartment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int DepartmentId { get; set; }

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
}
