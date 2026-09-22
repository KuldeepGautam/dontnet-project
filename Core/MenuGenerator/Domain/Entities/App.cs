namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy App_Name table (read-only) — confirmed against the DBA's
/// UBIS_RBAC.xlsx (2026-07-16), same pre-existing-legacy-table pattern as Role/Module/Function.
/// A prior pass incorrectly invented a brand-new "M_App" table for this; there is no such table —
/// App_Name already exists and is joined against by the still-legacy M_MapRoleModule.AppId
/// (renamed 2026-08-04 from M_RoleModuleMapping).
/// </summary>
public class App
{
    public int AppId { get; set; }
    public string AppName { get; set; } = string.Empty;

    /// <summary>"Y"/"N" — see MenuService.GetFullMenuAsync for the allow-list (== "Y") filter used here.</summary>
    public string Active { get; set; } = "N";
    public int PrintSeq { get; set; }

    /// <summary>
    /// Stable MVC Area segment, frozen once (db-scripts/AddStableSlugColumns_2026-07-22.sql +
    /// BackfillStableSlugs_2026-07-22.sql) and never recomputed from AppName afterward - see
    /// MenuService.BuildTree. Null only for an App added after that migration and not yet backfilled.
    /// </summary>
    public string? AreaSlug { get; set; }
}
