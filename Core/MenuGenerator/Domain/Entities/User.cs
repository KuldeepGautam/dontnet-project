namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// M_Users - dev/test-seed-only table. Not part of the production MenuGenerator schema
/// (that has no login concept); exists purely so the dev/test database has a demo login.
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
}
