namespace UBIS.Web.Models;

/// <summary>
/// Wraps the sidebar's node tree with whether a menu fetch has actually completed yet — needed
/// so the view can tell "not loaded yet, still fetching" apart from "loaded, and this role
/// genuinely has zero menu items," which an empty <see cref="Nodes"/> list alone can't distinguish.
/// Added 2026-07-20 when menu-fetching moved off the Login critical path onto a deferred AJAX call.
/// </summary>
public class SidebarMenuViewModel
{
    public bool MenuLoaded { get; set; }

    public List<MenuNodeViewModel> Nodes { get; set; } = new();

    /// <summary>Current request's Area/Controller/Action (added 2026-08-27 bug fix: "menu tree
    /// closes and shows only root nodes" after navigating - the sidebar is plain server-rendered
    /// HTML with no indication of which node is "current," so every full-page navigation rendered
    /// every &lt;details&gt; back to its default closed state). Compared against each
    /// <see cref="MenuNodeViewModel"/>'s own Area/Controller/Action so Default.cshtml can render
    /// the active leaf highlighted and every ancestor accordion already open, with no flash of
    /// collapsed state and no fragile client-side URL/href matching.</summary>
    public string? CurrentArea { get; set; }

    public string? CurrentController { get; set; }

    public string? CurrentAction { get; set; }
}
