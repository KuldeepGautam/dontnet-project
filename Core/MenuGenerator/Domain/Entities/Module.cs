namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy M_Module table (read-only).
/// </summary>
public class Module
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string Active { get; set; } = "N";

    /// <summary>
    /// Stable MVC Controller segment, frozen once and never recomputed from ModuleName afterward -
    /// see MenuService.BuildTree.
    /// </summary>
    public string? ControllerSlug { get; set; }
}
