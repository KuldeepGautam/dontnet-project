namespace UBIS.Services.MenuGenerator.Application.Interfaces;

using UBIS.Services.MenuGenerator.Application.DTOs;

public interface IMenuService
{
    /// <summary>
    /// Returns the App -> Module -> Function tree visible to the given role, or null if
    /// roleName does not resolve to an active row in M_Role.
    /// </summary>
    Task<MenuFullResponseDto?> GetFullMenuAsync(string roleName, CancellationToken ct = default);
}
