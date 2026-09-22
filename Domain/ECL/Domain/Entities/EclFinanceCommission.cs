namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only reference entity mapping the new dbo.M_FinanceCommission table (added
/// 2026-08-19) — replaces dbo.ECL_Config as the source for the Add Scheme Outlay page's selectable/
/// Outlay-column financial years. Period is the start year ("2026-2027"), NoYears is how many
/// consecutive years to generate (was hard-coded to 10 via ECL_Config.ECL_StartYear before; now
/// genuinely driven by NoYears).</summary>
public class EclFinanceCommission
{
    public int FinancialCommissionNo { get; set; }

    public string Period { get; set; } = string.Empty;

    public int NoYears { get; set; }

    /// <summary>Generates NoYears consecutive "YYYY-YYYY" financial-year strings starting at Period.</summary>
    public List<string> GenerateSelectableFinancialYears()
    {
        var years = new List<string>();
        if (string.IsNullOrWhiteSpace(Period) || Period.Length < 4 || !int.TryParse(Period[..4], out var startYear))
        {
            return years;
        }

        for (var i = 0; i < NoYears; i++)
        {
            var y = startYear + i;
            years.Add($"{y}-{y + 1}");
        }

        return years;
    }
}
