namespace UBIS.Services.Sbe.Infrastructure.Services;

/// <summary>Single point of reference for how a Scheme is labelled everywhere it's shown across SBE. Copied from PreBudget's own SchemeDisplayFormatter.</summary>
public static class SchemeDisplayFormatter
{
    public static string Format(int? schemeSrNo, string? schemeName) =>
        $"{schemeSrNo ?? 0} - {schemeName}";
}
