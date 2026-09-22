namespace UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Single point of reference for how a Sub-Scheme is labelled everywhere it's shown across the
/// PreBudget appendices (Appendix III/IV/IV-A/IV-B/VI-B all cascade Scheme -> SubScheme against the
/// same dbo.M_Scheme/M_SubScheme reference data). Client testing feedback (2026-08-24): "Display
/// Subscheme Sr No as SchemeSrNo - SubSchemeSrNo (2 digits) - Sub Scheme Name", e.g.
/// "4.03 - Less-Amount met From Fund for Innovation" - SubSchemeSrNo always padded to 2 digits for
/// values 0-9. Every appendix's own subschemes endpoint should call this instead of re-deriving the
/// format locally, so a future formatting change only has to happen in one place.
/// </summary>
public static class SubSchemeDisplayFormatter
{
    public static string Format(int? schemeSrNo, int? subSchemeSrNo, string subSchemeName) =>
        $"{schemeSrNo ?? 0}.{(subSchemeSrNo ?? 0).ToString("00")} - {subSchemeName}";
}
