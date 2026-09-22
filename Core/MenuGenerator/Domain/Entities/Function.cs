namespace UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Maps to the existing legacy M_Function table (read-only). AddressoftheFunction (aspx path)
/// is intentionally not mapped here since MVC routes by ModuleName/FunctionName instead.
/// Active re-confirmed against the DBA's UBIS_RBAC.xlsx (2026-07-16): this column does exist
/// (string "Y"/"N", both values present in real data) — a prior pass's 2026-07-13 export claimed
/// otherwise and dropped it; restored here. "Freez" still isn't mapped, this service doesn't need it.
/// </summary>
public class Function
{
    public int FunctionId { get; set; }
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>"Y"/"N" — see MenuService.GetFullMenuAsync for the deny-list (!= "N") filter used here.</summary>
    public string Active { get; set; } = "N";

    /// <summary>
    /// Stable MVC Action segment, frozen once and never recomputed from FunctionName afterward -
    /// see MenuService.BuildTree.
    /// </summary>
    public string? ActionSlug { get; set; }
}
