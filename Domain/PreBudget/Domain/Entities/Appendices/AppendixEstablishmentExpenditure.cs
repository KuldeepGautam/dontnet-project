namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix V: Estimates of Establishment &amp; Other Central Expenditure. Maps to dbo.AppendixEstablishmentExpenditure (was legacy Temp_EstExp). Own entry table, independent of V-A/V-B/V-C (decision 3).</summary>
public class AppendixEstablishmentExpenditure : AppendixEntityBase
{
    /// <summary>
    /// Legacy Temp_EstExp.Name - a fixed 6-value discriminator (see EstablishmentExpenditureCategory)
    /// the Budget Division's recommended RE/NBE figures are entered against, one row per category
    /// per demand+year (added 2026-07-31 to hold the legacy table's real shape; the approved
    /// AppendixVSummary.cshtml page is unaffected - it stays the separate read-only V-A/B/C rollup).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>
    /// Validates Category against the fixed EstablishmentExpenditureCategory.All allow-list, then
    /// copies every other field. Throws ArgumentException (distinct from the repository's
    /// InvalidOperationException for the frozen-check) so the controller can keep mapping this to
    /// its original Code = "VALIDATION_ERROR" response instead of "UPDATE_FAILED".
    /// </summary>
    public void UpdateFrom(
        string category,
        decimal? actuals,
        decimal? actualsUptoSeptPrevYear,
        decimal? be,
        decimal? actualsUptoSept,
        decimal? proposedRE,
        decimal? budgetRecommendedRE,
        decimal? proposedNBE,
        decimal? budgetRecommendedNBE,
        string? remarksBudget)
    {
        if (!EstablishmentExpenditureCategory.All.Contains(category))
        {
            throw new ArgumentException($"Category must be one of: {string.Join(", ", EstablishmentExpenditureCategory.All)}.");
        }

        Category = category;
        Actuals = actuals;
        ActualsUptoSeptPrevYear = actualsUptoSeptPrevYear;
        BE = be;
        ActualsUptoSept = actualsUptoSept;
        ProposedRE = proposedRE;
        BudgetRecommendedRE = budgetRecommendedRE;
        ProposedNBE = proposedNBE;
        BudgetRecommendedNBE = budgetRecommendedNBE;
        RemarksBudget = remarksBudget;
    }
}

public static class EstablishmentExpenditureCategory
{
    public const string Salary = "Salary";
    public const string NonSalary = "Non-Salary";
    public const string GiaGeneral = "GiA General";
    public const string GiaCca = "GiA CCA";
    public const string GiaSalary = "GiA Salary";
    public const string OtherThanAB = "Other than AB";

    public static readonly string[] All = [Salary, NonSalary, GiaGeneral, GiaCca, GiaSalary, OtherThanAB];
}
