namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy M_Role table (read-only, owned by AIM's DB, not this service).
/// </summary>
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Active { get; set; } = "N";
}
