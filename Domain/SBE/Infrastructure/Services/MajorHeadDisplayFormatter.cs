namespace UBIS.Services.Sbe.Infrastructure.Services;

/// <summary>
/// Single point of reference for how a Major Head is labelled and ordered everywhere it's shown
/// across SBE. Copied from PreBudget's own MajorHeadDisplayFormatter (client convention, applied
/// to SBE from day one per 2026-08-25 direction): "Code - Name" display, ordered numerically by
/// MajorHeadCode (a varchar column, so a plain string ORDER BY sorts "10" before "9" —
/// NumericSortKey parses it for a correct numeric sort instead). Every SBE endpoint that shows a
/// Major Head dropdown should call these instead of re-deriving the format/sort locally.
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
