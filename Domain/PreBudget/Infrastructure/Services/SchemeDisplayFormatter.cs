namespace UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Single point of reference for how a Scheme is labelled everywhere it's shown across the
/// PreBudget appendices (III/IV/IV-A/IV-B/VI-B all cascade against dbo.M_Scheme). Client testing
/// feedback (2026-08-25): "Scheme should show SchemeSrNo - SchemeName, order by SchemeSrNo" - every
/// appendix's own scheme dropdown/grid should call this instead of re-deriving the format locally,
/// so a future formatting change only has to happen in one place (same pattern as
/// <see cref="SubSchemeDisplayFormatter"/> and <see cref="MajorHeadDisplayFormatter"/>).
/// </summary>
public static class SchemeDisplayFormatter
{
    public static string Format(int? schemeSrNo, string? schemeName) =>
        $"{schemeSrNo ?? 0} - {schemeName}";
}
