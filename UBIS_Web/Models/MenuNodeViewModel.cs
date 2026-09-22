namespace UBIS.Web.Models;

using UBIS.Web.Services.Clients;

/// <summary>
/// Uniform tree (App -> Module -> Function) for the sidebar accordion partial. Only leaf nodes
/// are navigable; Area/Controller/Action map 1:1 to MenuGenerator's AreaSlug/ControllerSlug/
/// ActionSlug, so a leaf's Url is exactly the MVC route to render. Not every Module has Functions
/// under it — a childless Module is itself a leaf (Action "Index" on that Module's controller,
/// scaffolded the same way by UBIS_Web/Tools/MenuScaffolder), rather than an always-empty accordion.
/// </summary>
public class MenuNodeViewModel
{
    public string Title { get; set; } = string.Empty;

    public string? AreaName { get; set; }

    public string? ControllerName { get; set; }

    public string? ActionName { get; set; }

    public bool IsLeaf => ActionName != null;

    public List<MenuNodeViewModel> Children { get; set; } = new();

    public static List<MenuNodeViewModel> FromMenuResponse(MenuFullResponseDto menu) =>
        menu.Apps.Select(app => new MenuNodeViewModel
        {
            Title = app.AppName,
            AreaName = app.AreaSlug,
            Children = app.Modules.Select(module => module.Functions.Count == 0
                ? new MenuNodeViewModel
                {
                    Title = module.ModuleName,
                    AreaName = app.AreaSlug,
                    ControllerName = module.ControllerSlug,
                    ActionName = "Index"
                }
                : new MenuNodeViewModel
                {
                    Title = module.ModuleName,
                    AreaName = app.AreaSlug,
                    ControllerName = module.ControllerSlug,
                    Children = module.Functions.Select(function => new MenuNodeViewModel
                    {
                        Title = function.FunctionName,
                        AreaName = app.AreaSlug,
                        ControllerName = module.ControllerSlug,
                        ActionName = function.ActionSlug
                    }).ToList()
                }).ToList()
        }).ToList();
}
