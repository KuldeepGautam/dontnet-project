namespace UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Single point of reference for how a Major Head is labelled and ordered everywhere it's shown
/// across the PreBudget appendices (VII-A/VII-B/PA all cascade against dbo.M_MajorHead). Client
/// testing feedback (2026-08-24): "All Appendix Major Head DropDown Display: MajorHeadCode - Major
/// Head Name, order by MajorHeadCode numeric" - MajorHeadCode is a varchar column, so a plain
/// string ORDER BY sorts "10" before "9"; NumericSortKey parses it for a correct numeric sort
/// instead. Every appendix's own major-heads endpoint should call these instead of re-deriving the
/// format/sort locally, so a future change only has to happen in one place.
/// </summary>
public static class MajorHeadDisplayFormatter
{
    public static string Format(string? majorHeadCode, string? majorHeadName) =>
        $"{majorHeadCode} - {majorHeadName}";

    /// <summary>Parses MajorHeadCode as an integer for numeric ordering; non-numeric codes (rare,
    /// legacy data) sort last rather than throwing.</summary>
    public static int NumericSortKey(string? majorHeadCode) =>
        int.TryParse(majorHeadCode, out var value) ? value : int.MaxValue;
}
