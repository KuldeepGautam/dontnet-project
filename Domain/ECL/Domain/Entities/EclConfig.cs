namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Maps to dbo.ECL_Config — single-row config table. ECL_StartYear drives the 10 selectable
/// financial years, generated dynamically at read time (never hard-coded, never from
/// dbo.M_FinancialYear).
/// </summary>
public class EclConfig
{
    public int Id { get; set; }

    public string EclStartYear { get; set; } = string.Empty;

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }

    /// <summary>Generates the 10 consecutive "YYYY-YYYY" financial-year strings starting at EclStartYear.</summary>
    public List<string> GenerateSelectableFinancialYears()
    {
        var years = new List<string>();
        if (string.IsNullOrWhiteSpace(EclStartYear) || EclStartYear.Length < 4 || !int.TryParse(EclStartYear[..4], out var startYear))
        {
            return years;
        }

        for (var i = 0; i < 10; i++)
        {
            var y = startYear + i;
            years.Add($"{y}-{y + 1}");
        }

        return years;
    }
}
