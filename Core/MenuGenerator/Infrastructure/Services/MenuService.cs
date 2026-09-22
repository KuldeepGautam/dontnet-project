namespace UBIS.Services.MenuGenerator.Infrastructure.Services;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.MenuGenerator.Application.DTOs;
using UBIS.Services.MenuGenerator.Application.Interfaces;
using UBIS.Services.MenuGenerator.Infrastructure.Persistence;

public class MenuService : IMenuService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static readonly Regex NonAlphaNumeric = new("[^A-Za-z0-9]", RegexOptions.Compiled);

    private readonly MenuDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogWriterClient _logWriter;

    public MenuService(MenuDbContext db, ICacheService cache, ILogWriterClient logWriter)
    {
        _db = db;
        _cache = cache;
        _logWriter = logWriter;
    }

    public async Task<MenuFullResponseDto?> GetFullMenuAsync(string roleName, CancellationToken ct = default)
    {
        var cacheKey = $"menu:full:{roleName.Trim().ToUpperInvariant()}";

        var cached = await _cache.GetAsync<MenuFullResponseDto>(cacheKey, ct).ConfigureAwait(false);
        if (cached != null)
        {
            return cached;
        }

        // Legacy data export rule (compliance brief, 2026-07-10): treat every M_ master/mapping
        // Active flag as a deny-list ("!= N" admits Y, null, blank, or any other value) rather
        // than an allow-list ("== Y"), so historical rows aren't silently dropped just because
        // they were never explicitly marked "Y". Applies below to Role/Module/RoleModuleMapping/
        // RoleFunctionMapping/Function.Active.
        //
        // App_Name.Active is the one deliberate exception — allow-list ("== Y"), per direct
        // instruction, confirmed against the DBA's UBIS_RBAC.xlsx (2026-07-16): every real row is
        // currently "Y" with no blank/null case to worry about. That same xlsx also confirmed
        // M_Function.Active is a real column after all (a prior pass's 2026-07-13 export claimed
        // it didn't exist) — now included below as a deny-list check like its siblings.
        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleName == roleName && r.Active != "N", ct)
            .ConfigureAwait(false);

        if (role == null)
        {
            await _logWriter.WarnAsync(
                $"RoleName '{roleName}' not found or inactive in M_Role",
                ct: ct).ConfigureAwait(false);
            return null;
        }

        var rows = await (
            from rm in _db.RoleModuleMappings.AsNoTracking()
            where rm.RoleId == role.RoleId && rm.Active != "N" && rm.RMFreez != "Y"
            join module in _db.Modules.AsNoTracking() on rm.ModuleId equals module.ModuleId
            join app in _db.Apps.AsNoTracking() on rm.AppId equals app.AppId
            join rf in _db.RoleFunctionMappings.AsNoTracking()
                on new { rm.RoleId, rm.ModuleId } equals new { rf.RoleId, rf.ModuleId }
            join function in _db.Functions.AsNoTracking() on rf.FunctionId equals function.FunctionId
            where module.Active != "N" && app.Active == "Y" && rf.Active != "N" && rf.RFFreez != "Y" && function.Active != "N"
            orderby app.PrintSeq, rm.RmSequenceNo, rf.RfSequenceNo
            select new MenuFlatRow(
                app.AppId, app.AppName, app.AreaSlug,
                module.ModuleId, module.ModuleName, module.ControllerSlug,
                function.FunctionId, function.FunctionName, function.ActionSlug)
        ).ToListAsync(ct).ConfigureAwait(false);

        var result = await BuildTreeAsync(rows, ct).ConfigureAwait(false);

        if (result.Apps.Count == 0)
        {
            await _logWriter.WarnAsync(
                $"Role '{roleName}' (RoleId={role.RoleId}) resolved but has zero visible apps/modules/functions",
                ct: ct).ConfigureAwait(false);
        }

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct).ConfigureAwait(false);

        return result;
    }

    // Routing (Area/Controller/Action) is decoupled from display names (AppName/ModuleName/
    // FunctionName) as of 2026-07-22 - see db-scripts/AddStableSlugColumns_2026-07-22.sql. Renaming
    // an App/Module/Function used to break every existing UBIS_Web page under it, since the slug
    // (and therefore the URL) was recomputed fresh from the current name on every single request;
    // UBIS_Web's scaffolded controller/view files don't move when a display name changes.
    //
    // Now: App.AreaSlug / Module.ControllerSlug / Function.ActionSlug are persisted columns, read
    // here as the primary source of routing truth. They're set once - either by the one-time
    // backfill migration (everything that already had a live route before this fix), or here, the
    // first time a brand-new (never-backfilled) App/Module/Function shows up in a generated menu -
    // and never recomputed from the display name again afterward. A rename only ever changes the
    // label shown in the menu; the URL stays exactly what it was frozen to.
    private async Task<MenuFullResponseDto> BuildTreeAsync(List<MenuFlatRow> rows, CancellationToken ct)
    {
        var newAppSlugs = new Dictionary<int, string>();
        var newModuleSlugs = new Dictionary<int, string>();
        var newFunctionSlugs = new Dictionary<int, string>();

        string ResolveAppSlug(int appId, string appName, string? persisted)
        {
            if (!string.IsNullOrEmpty(persisted)) return persisted;
            return newAppSlugs.TryGetValue(appId, out var existing) ? existing : newAppSlugs[appId] = Slugify(appName);
        }

        string ResolveModuleSlug(int moduleId, string moduleName, string? persisted)
        {
            if (!string.IsNullOrEmpty(persisted)) return persisted;
            return newModuleSlugs.TryGetValue(moduleId, out var existing) ? existing : newModuleSlugs[moduleId] = Slugify(moduleName);
        }

        string ResolveFunctionSlug(int functionId, string functionName, string? persisted)
        {
            if (!string.IsNullOrEmpty(persisted)) return persisted;
            return newFunctionSlugs.TryGetValue(functionId, out var existing) ? existing : newFunctionSlugs[functionId] = Slugify(functionName);
        }

        var apps = rows
            .GroupBy(r => (r.AppId, r.AppName, r.AppAreaSlug))
            .Select(appGroup =>
            {
                var areaSlug = ResolveAppSlug(appGroup.Key.AppId, appGroup.Key.AppName, appGroup.Key.AppAreaSlug);

                return new MenuAppDto(
                    appGroup.Key.AppId,
                    appGroup.Key.AppName,
                    areaSlug,
                    appGroup
                        .GroupBy(r => (r.ModuleId, r.ModuleName, r.ModuleControllerSlug))
                        .Select(moduleGroup =>
                        {
                            var controllerSlug = ResolveModuleSlug(moduleGroup.Key.ModuleId, moduleGroup.Key.ModuleName, moduleGroup.Key.ModuleControllerSlug);

                            var functions = moduleGroup
                                .GroupBy(r => (r.FunctionId, r.FunctionName, r.FunctionActionSlug))
                                .Select(fg =>
                                {
                                    var actionSlug = ResolveFunctionSlug(fg.Key.FunctionId, fg.Key.FunctionName, fg.Key.FunctionActionSlug);
                                    return new MenuFunctionDto(
                                        fg.Key.FunctionId,
                                        fg.Key.FunctionName,
                                        actionSlug,
                                        $"/{areaSlug}/{controllerSlug}/{actionSlug}");
                                })
                                .ToList();

                            return new MenuModuleDto(moduleGroup.Key.ModuleId, moduleGroup.Key.ModuleName, controllerSlug, functions);
                        })
                        .ToList());
            })
            .ToList();

        // Freeze any slugs computed for the first time just now, so they never shift again.
        if (newAppSlugs.Count > 0 || newModuleSlugs.Count > 0 || newFunctionSlugs.Count > 0)
        {
            await PersistNewSlugsAsync(newAppSlugs, newModuleSlugs, newFunctionSlugs, ct).ConfigureAwait(false);
        }

        return new MenuFullResponseDto(apps);
    }

    private async Task PersistNewSlugsAsync(
        Dictionary<int, string> newAppSlugs,
        Dictionary<int, string> newModuleSlugs,
        Dictionary<int, string> newFunctionSlugs,
        CancellationToken ct)
    {
        try
        {
            foreach (var (appId, slug) in newAppSlugs)
            {
                await _db.Apps.Where(a => a.AppId == appId && a.AreaSlug == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.AreaSlug, slug), ct).ConfigureAwait(false);
            }

            foreach (var (moduleId, slug) in newModuleSlugs)
            {
                await _db.Modules.Where(m => m.ModuleId == moduleId && m.ControllerSlug == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.ControllerSlug, slug), ct).ConfigureAwait(false);
            }

            foreach (var (functionId, slug) in newFunctionSlugs)
            {
                await _db.Functions.Where(f => f.FunctionId == functionId && f.ActionSlug == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.ActionSlug, slug), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Freezing is best-effort: if it fails (e.g. a transient DB hiccup), the menu response
            // already went out correctly with the freshly-computed slug - just means this specific
            // App/Module/Function will recompute (deterministically, same result) again next time
            // until a freeze attempt succeeds. Never let this break the actual menu response.
            await _logWriter.WarnAsync("Failed to persist newly-computed slug(s) - will retry next request.", new { ex.Message }, ct).ConfigureAwait(false);
        }
    }

    // A slug becomes both a C# identifier (MenuScaffolder-generated controller/action names) and
    // an MVC route segment, so it must be a valid identifier — plain alphanumeric stripping alone
    // isn't enough when the source name starts with a digit (e.g. "10A10B and 10BB Statement wise
    // Scheme" -> "10A10Band10BBStatementwiseScheme", which "public IActionResult 10A10B...()"
    // fails to compile on). Prefixing an underscore keeps the slug stable/deterministic (same
    // input always produces the same slug) while guaranteeing it starts with a valid character.
    private static string Slugify(string name)
    {
        var slug = NonAlphaNumeric.Replace(name, string.Empty);
        return slug.Length > 0 && char.IsDigit(slug[0]) ? "_" + slug : slug;
    }

    private sealed record MenuFlatRow(
        int AppId, string AppName, string? AppAreaSlug,
        int ModuleId, string ModuleName, string? ModuleControllerSlug,
        int FunctionId, string FunctionName, string? FunctionActionSlug);
}
