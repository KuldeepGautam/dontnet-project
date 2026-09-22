namespace UBIS.Web.Services.Session;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UBIS.Web.Models;

/// <summary>
/// Client requirement 2026-08-31 ("the page display route should [be] made from menu/db, wrongly
/// on page: Pre Budget Meeting / Pre Budget Meeting / Autonomous Master for Appendix VI C and VI E
/// ... this is issue for all pages"): every page's breadcrumb was a hardcoded string literal set
/// per controller action (250+ views across every Area read <c>@ViewData["Breadcrumb"]</c>, each
/// fed by its own action's own hand-typed string) - free to drift from the real menu hierarchy, as
/// this exact page had (its "Pre Budget Meeting / Pre Budget Meeting / ..." repeated the App name
/// twice instead of showing App / Module / Function).
///
/// Runs on every request as a global result filter (same registration pattern as
/// TokenRefreshFilter/ComplianceInterceptorFilter in Program.cs) - after the action method (so any
/// hardcoded ViewData["Breadcrumb"] a controller still sets is simply overwritten, no per-action
/// cleanup required for the fix to take effect) but before the view renders. Walks the same
/// App/Module/Function menu tree the sidebar itself is built from (session.Menu, cached at login -
/// see SidebarMenuViewComponent) to find the node matching the current request's Area/Controller/
/// Action, and joins that node's ancestor Titles with " / " - the menu tree IS the DB-driven
/// App/Module/Function hierarchy (MenuGenerator), so this breadcrumb can never again drift from
/// what the sidebar itself shows.
///
/// Deliberately leaves ViewData["Breadcrumb"] untouched when no matching menu node is found (e.g.
/// Login, Dashboard, Admin pages, or any route not present in this user's menu tree at all) -
/// those pages' own hardcoded breadcrumb (or lack of one) is preserved rather than being blanked
/// out.
/// </summary>
public class BreadcrumbFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult viewResult)
        {
            var session = context.HttpContext.Session.GetUbisSession();
            if (session?.Menu != null)
            {
                var area = context.RouteData.Values["area"]?.ToString();
                var controller = context.RouteData.Values["controller"]?.ToString();
                // Bug report 2026-08-31 (REMeetingAllocation: breadcrumb reads "Pre-Budget - Add
                // Allocation" on the initial GET, but silently changes to "Add Allocation" after
                // any POST that redisplays the same page - e.g. UpdateAllocation/SubmitAllocation):
                // this filter matches menu nodes by the CURRENT request's own Action name, but a
                // write action like "UpdateAllocation" is a POST endpoint, never itself a menu
                // Function - it just calls `return View("REMeetingAllocation", model)` to redisplay
                // the same GET page's view under a different action name. FindPath below then finds
                // no match and (by design, see the class doc comment) leaves ViewData untouched,
                // falling back to whatever hardcoded string that write action set - which is exactly
                // the kind of per-action-typed-string drift this filter exists to eliminate. A
                // controller action can opt out of using its own literal name for this lookup by
                // setting HttpContext.Items["BreadcrumbAction"] first (see AllocationController's
                // SubmitAllocation/UpdateAllocation) to the name of the GET action whose page it's
                // really redisplaying.
                var action = (context.HttpContext.Items.TryGetValue("BreadcrumbAction", out var overrideAction) ? overrideAction as string : null)
                    ?? context.RouteData.Values["action"]?.ToString();

                var nodes = MenuNodeViewModel.FromMenuResponse(session.Menu);
                var path = FindPath(nodes, area, controller, action);
                if (path != null)
                {
                    viewResult.ViewData["Breadcrumb"] = string.Join(" / ", path.Select(n => n.Title));
                }
            }
        }

        await next();
    }

    /// <summary>Depth-first search for the leaf node matching the given Area/Controller/Action
    /// (case-insensitive - MenuGenerator's slugs and MVC's RouteData casing aren't guaranteed to
    /// match exactly). Returns the full root-to-leaf ancestor chain (App -> Module -> Function) so
    /// the caller can join every level's Title, or null if this route isn't in the menu tree at
    /// all.</summary>
    private static List<MenuNodeViewModel>? FindPath(List<MenuNodeViewModel> nodes, string? area, string? controller, string? action)
    {
        foreach (var node in nodes)
        {
            var isMatch = node.IsLeaf
                && string.Equals(node.AreaName, area, StringComparison.OrdinalIgnoreCase)
                && string.Equals(node.ControllerName, controller, StringComparison.OrdinalIgnoreCase)
                && string.Equals(node.ActionName, action, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                return new List<MenuNodeViewModel> { node };
            }

            if (node.Children.Count > 0)
            {
                var childPath = FindPath(node.Children, area, controller, action);
                if (childPath != null)
                {
                    childPath.Insert(0, node);
                    return childPath;
                }
            }
        }

        return null;
    }
}
