// MenuScaffolder: reads the live role-scoped menu tree from AIM + MenuGenerator and
// generates one real MVC Area/Controller/Action + starter Razor view per Function, so
// every link the sidebar renders resolves to a real page from day one. Idempotent:
// existing views are never overwritten, and existing controllers are only appended to
// if they still carry the "SCAFFOLD:APPEND-ACTIONS-ABOVE-THIS-LINE" marker.
//
// Usage (from UBIS_Web/Tools/MenuScaffolder):
//   dotnet run -- --aim-url http://localhost:5101 --menu-url http://localhost:5102 --internal-key <shared-secret> --output ../../

using System.Net.Http.Headers;
using System.Text.Json;
using UBIS.Web.Tools.MenuScaffolder;

var options = ScaffoldOptions.Parse(args);
Console.WriteLine($"AIM:            {options.AimUrl}");
Console.WriteLine($"MenuGenerator:  {options.MenuUrl}");
Console.WriteLine($"UBIS_Web root:  {Path.GetFullPath(options.OutputRoot)}");
Console.WriteLine();

using var http = new HttpClient();
if (!string.IsNullOrEmpty(options.InternalKey))
{
    http.DefaultRequestHeaders.Add(options.InternalHeaderName, options.InternalKey);
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

var roles = await GetAllRolesAsync(http, options.AimUrl, jsonOptions);
Console.WriteLine($"Discovered {roles.Count} role(s).");

var routes = new Dictionary<(string Area, string Controller), List<RouteEntry>>();
var seenActions = new HashSet<(string Area, string Controller, string Action)>();

foreach (var role in roles)
{
    var menu = await GetFullMenuAsync(http, options.MenuUrl, role.RoleName, jsonOptions);
    if (menu == null)
    {
        continue;
    }

    void AddRoute(string areaSlug, string controllerSlug, string actionSlug, string appName, string moduleName, string functionName)
    {
        var key = (areaSlug, controllerSlug);
        var actionKey = (areaSlug, controllerSlug, actionSlug);
        if (!seenActions.Add(actionKey))
        {
            return;
        }

        if (!routes.TryGetValue(key, out var list))
        {
            list = new List<RouteEntry>();
            routes[key] = list;
        }

        list.Add(new RouteEntry(areaSlug, controllerSlug, actionSlug, appName, moduleName, functionName));
    }

    foreach (var app in menu.Apps)
    {
        foreach (var module in app.Modules)
        {
            if (module.Functions.Count == 0)
            {
                // Not every Module has Functions under it — the Module itself is the leaf link
                // in that case (Area=App, Controller=Module, Action=Index), rather than producing
                // zero routes and leaving the sidebar entry dead.
                AddRoute(app.AreaSlug, module.ControllerSlug, "Index", app.AppName, module.ModuleName, module.ModuleName);
                continue;
            }

            foreach (var function in module.Functions)
            {
                AddRoute(app.AreaSlug, module.ControllerSlug, function.ActionSlug, app.AppName, module.ModuleName, function.FunctionName);
            }
        }
    }
}

Console.WriteLine($"Discovered {routes.Count} distinct Area/Controller pair(s) across {seenActions.Count} action(s).");
Console.WriteLine();

var stats = new ScaffoldStats();
foreach (var ((area, controller), entries) in routes)
{
    EnsureAreaShellFiles(options.OutputRoot, area, stats);
    ScaffoldController(options.OutputRoot, area, controller, entries, stats);
    foreach (var entry in entries)
    {
        ScaffoldView(options.OutputRoot, entry, stats);
    }
}

Console.WriteLine();
Console.WriteLine($"New controllers:  {stats.NewControllers}");
Console.WriteLine($"Appended actions: {stats.AppendedActions}");
Console.WriteLine($"New views:        {stats.NewViews}");
Console.WriteLine($"Skipped (customized, no marker): {stats.SkippedCustomized}");

static async Task<List<RoleDetailDto>> GetAllRolesAsync(HttpClient http, string aimUrl, JsonSerializerOptions jsonOptions)
{
    using var response = await http.GetAsync($"{aimUrl.TrimEnd('/')}/api/userrole/all-roles");
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"WARNING: could not fetch roles from AIM ({(int)response.StatusCode}). Aborting.");
        return new List<RoleDetailDto>();
    }

    var body = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<List<RoleDetailDto>>(body, jsonOptions) ?? new List<RoleDetailDto>();
}

static async Task<MenuFullResponseDto?> GetFullMenuAsync(HttpClient http, string menuUrl, string roleName, JsonSerializerOptions jsonOptions)
{
    using var response = await http.GetAsync($"{menuUrl.TrimEnd('/')}/api/menu/by-role?roleName={Uri.EscapeDataString(roleName)}");
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"  Role '{roleName}': no menu ({(int)response.StatusCode}), skipping.");
        return null;
    }

    var body = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<MenuFullResponseDto>(body, jsonOptions);
}

static void EnsureAreaShellFiles(string root, string area, ScaffoldStats stats)
{
    var viewsDir = Path.Combine(root, "Areas", area, "Views");
    Directory.CreateDirectory(viewsDir);

    var viewStart = Path.Combine(viewsDir, "_ViewStart.cshtml");
    if (!File.Exists(viewStart))
    {
        File.WriteAllText(viewStart, "@{\n    Layout = \"~/Views/Shared/_Layout.cshtml\";\n}\n");
        stats.NewViews++;
    }

    var viewImports = Path.Combine(viewsDir, "_ViewImports.cshtml");
    if (!File.Exists(viewImports))
    {
        File.WriteAllText(viewImports,
            "@using UBIS.Web\n@using UBIS.Web.Models\n@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers\n");
        stats.NewViews++;
    }
}

static void ScaffoldController(string root, string area, string controller, List<RouteEntry> entries, ScaffoldStats stats)
{
    const string marker = "    // SCAFFOLD:APPEND-ACTIONS-ABOVE-THIS-LINE";

    var controllerDir = Path.Combine(root, "Areas", area, "Controllers");
    Directory.CreateDirectory(controllerDir);
    var filePath = Path.Combine(controllerDir, $"{controller}Controller.cs");

    if (!File.Exists(filePath))
    {
        var actionsText = string.Join("\n\n", entries.Select(BuildActionMethod));
        var content = $$"""
// <auto-generated-by-menu-scaffolder>
// Re-running MenuScaffolder only appends new actions below the marker comment near
// the bottom of this class; it never rewrites an existing method body. Remove the
// marker to opt this file out of future auto-appends entirely.
namespace UBIS.Web.Areas.{{area}}.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("{{area}}")]
[Authorize]
public class {{controller}}Controller : Controller
{
{{actionsText}}

{{marker}}
}

""";
        File.WriteAllText(filePath, content);
        stats.NewControllers++;
        Console.WriteLine($"  + Controller {area}/{controller} ({entries.Count} action(s))");
        return;
    }

    var existing = File.ReadAllText(filePath);
    var missing = entries.Where(e => !existing.Contains($"IActionResult {e.ActionSlug}(")).ToList();
    if (missing.Count == 0)
    {
        return;
    }

    var markerIndex = existing.IndexOf(marker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
        Console.WriteLine($"  ! Skipping {area}/{controller}Controller.cs: marker not found (file appears customized). {missing.Count} action(s) not added: {string.Join(", ", missing.Select(m => m.ActionSlug))}");
        stats.SkippedCustomized++;
        return;
    }

    var appendText = string.Join("\n\n", missing.Select(BuildActionMethod)) + "\n\n";
    var updated = existing.Insert(markerIndex, appendText);
    File.WriteAllText(filePath, updated);
    stats.AppendedActions += missing.Count;
    Console.WriteLine($"  ~ Appended {missing.Count} action(s) to {area}/{controller}Controller.cs");
}

static string BuildActionMethod(RouteEntry entry) => $$"""
    // App: {{entry.AppName}} | Module: {{entry.ModuleName}}
    public IActionResult {{entry.ActionSlug}}()
    {
        ViewData["Title"] = "{{entry.FunctionName}}";
        ViewData["Breadcrumb"] = "{{entry.AppName}} / {{entry.ModuleName}} / {{entry.FunctionName}}";
        return View();
    }
""";

static void ScaffoldView(string root, RouteEntry entry, ScaffoldStats stats)
{
    var viewDir = Path.Combine(root, "Areas", entry.AreaSlug, "Views", entry.ControllerSlug);
    Directory.CreateDirectory(viewDir);
    var viewPath = Path.Combine(viewDir, $"{entry.ActionSlug}.cshtml");

    if (File.Exists(viewPath))
    {
        return;
    }

    var content = $$"""
@{
    ViewData["Title"] = ViewData["Title"] ?? "{{entry.FunctionName}}";
}

<div class="rounded-2xl border border-slate-200 bg-white p-6 shadow-soft dark:border-slate-800 dark:bg-slate-900">
    <p class="text-xs font-bold uppercase tracking-wide text-teal">@ViewData["Breadcrumb"]</p>
    <h1 class="mt-1 text-xl font-black text-navy dark:text-white">{{entry.FunctionName}}</h1>
    <p class="mt-4 text-sm text-slate-500">
        This data-entry page is pending design. It was generated automatically from the live
        menu so the link works today &mdash; replace this body with the real form once designed.
    </p>
</div>

@* Once a real data-entry <form> exists on this page, give it a stable autosave key so
   wwwroot/js/autosave.js starts protecting it against network drops / forced sign-outs —
   no other wiring is needed:

   <form method="post" data-autosave-key="{{entry.AreaSlug}}-{{entry.ControllerSlug}}-{{entry.ActionSlug}}">
       @Html.AntiForgeryToken()
       ...
   </form>
*@
""";
    File.WriteAllText(viewPath, content);
    stats.NewViews++;
}

sealed class ScaffoldStats
{
    public int NewControllers;
    public int AppendedActions;
    public int NewViews;
    public int SkippedCustomized;
}

sealed class ScaffoldOptions
{
    public string AimUrl { get; init; } = "http://localhost:5101";
    public string MenuUrl { get; init; } = "http://localhost:5102";
    public string InternalHeaderName { get; init; } = "X-UBIS-Internal-Client-Key";
    public string InternalKey { get; init; } = string.Empty;
    public string OutputRoot { get; init; } = "../../";

    public static ScaffoldOptions Parse(string[] args)
    {
        string aimUrl = "http://localhost:5101";
        string menuUrl = "http://localhost:5102";
        string headerName = "X-UBIS-Internal-Client-Key";
        string key = string.Empty;
        string output = "../../";

        for (var i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--aim-url": aimUrl = args[++i]; break;
                case "--menu-url": menuUrl = args[++i]; break;
                case "--internal-header": headerName = args[++i]; break;
                case "--internal-key": key = args[++i]; break;
                case "--output": output = args[++i]; break;
            }
        }

        return new ScaffoldOptions
        {
            AimUrl = aimUrl,
            MenuUrl = menuUrl,
            InternalHeaderName = headerName,
            InternalKey = key,
            OutputRoot = output
        };
    }
}
