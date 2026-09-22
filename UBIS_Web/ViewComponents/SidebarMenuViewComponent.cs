namespace UBIS.Web.ViewComponents;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Models;
using UBIS.Web.Services.Session;

/// <summary>
/// Renders the left-hand 3-level accordion from the menu tree cached in the user's
/// Redis-backed Session at login — every authenticated page shows the same sidebar
/// without each controller action needing to pass it through its own view model.
/// </summary>
public class SidebarMenuViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var session = HttpContext.Session.GetUbisSession();
        // Bug fix 2026-08-27 ("menu tree closes and shows only root nodes" on every navigation):
        // RouteData here is the CURRENT page's own route (the same request the layout/sidebar is
        // rendering for), not some pseudo-route for the ViewComponent itself - Default.cshtml uses
        // this to keep the active item's ancestor accordions open and highlight the selected row.
        return View(new SidebarMenuViewModel
        {
            MenuLoaded = session?.Menu != null,
            Nodes = session?.Menu != null ? MenuNodeViewModel.FromMenuResponse(session.Menu) : new List<MenuNodeViewModel>(),
            CurrentArea = RouteData.Values["area"]?.ToString(),
            CurrentController = RouteData.Values["controller"]?.ToString(),
            CurrentAction = RouteData.Values["action"]?.ToString()
        });
    }
}
