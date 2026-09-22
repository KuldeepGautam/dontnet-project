namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>
/// Appendix III: CNA/SNA Balances of Schemes. Maps to dbo.AppendixCnaSnaBalance (was legacy
/// Temp_IIICNASNA). CategoryType discriminates the FRS's 3 "Balance Type" variants in one table.
/// SchemeId/SubSchemeId are external ReferenceData ids (validated over HTTP), not local FKs.
/// </summary>
public class AppendixCnaSnaBalance : AppendixEntityBase
{
    public string CategoryType { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? BE { get; set; }
    public decimal? BalanceAsOnAprilOpening { get; set; }
    public decimal? ReleasesDuringFY { get; set; }
    public decimal? BalanceAsOnSeptClosing { get; set; }
    public DateOnly? DateOfLastRelease { get; set; }
    public decimal? AmountOfLastRelease { get; set; }
    public decimal? NotTransferredToSnaAsOnSept { get; set; }
    public string? Remarks { get; set; }
    public string? ReasonForExemption { get; set; }

    /// <summary>
    /// Validates CategoryType (see CnaSnaCategoryType.All - must match
    /// CK_Appendix_III_CnaSnaBalance_CategoryType, the DBA-owned CHECK constraint) then copies every
    /// other field from the request. Throws ArgumentException (distinct from the repository's
    /// InvalidOperationException for the frozen-check) so the controller can keep mapping this to
    /// its original Code = "VALIDATION_ERROR" response instead of "UPDATE_FAILED".
    /// </summary>
    public void UpdateFrom(
        string categoryType,
        int? schemeId,
        int? subSchemeId,
        decimal? be,
        decimal? balanceAsOnAprilOpening,
        decimal? releasesDuringFY,
        decimal? balanceAsOnSeptClosing,
        DateOnly? dateOfLastRelease,
        decimal? amountOfLastRelease,
        decimal? notTransferredToSnaAsOnSept,
        string? remarks,
        string? reasonForExemption)
    {
        if (string.IsNullOrWhiteSpace(categoryType))
        {
            throw new ArgumentException("Balance Type (CategoryType) is required.");
        }

        if (!CnaSnaCategoryType.All.Contains(categoryType))
        {
            throw new ArgumentException($"Balance Type (CategoryType) must be one of: {string.Join(", ", CnaSnaCategoryType.All)}.");
        }

        CategoryType = categoryType;
        SchemeId = schemeId;
        SubSchemeId = subSchemeId;
        BE = be;
        BalanceAsOnAprilOpening = balanceAsOnAprilOpening;
        ReleasesDuringFY = releasesDuringFY;
        BalanceAsOnSeptClosing = balanceAsOnSeptClosing;
        DateOfLastRelease = dateOfLastRelease;
        AmountOfLastRelease = amountOfLastRelease;
        NotTransferredToSnaAsOnSept = notTransferredToSnaAsOnSept;
        Remarks = remarks;
        ReasonForExemption = reasonForExemption;
    }
}

/// <summary>
/// Must match CK_Appendix_III_CnaSnaBalance_CategoryType (DBA-owned constraint on
/// dbo.Appendix_III_CnaSnaBalance, widened 2026-07-29 to these 4 values) - validated here so an
/// invalid value gets a clean 400 instead of an unhandled SqlException/500 from the CHECK
/// constraint. Kept in sync with AppendixIIIController.AllowedCategoryTypes, which additionally
/// carries the display Label for the dropdown (a WebApi/UI concern, not a domain invariant).
/// </summary>
public static class CnaSnaCategoryType
{
    public const string CentralSectorScheme = "CentralSectorScheme";
    public const string CentrallySponsoredScheme = "CentrallySponsoredScheme";
    public const string ExemptedFromCna = "ExemptedFromCna";
    public const string ExemptedFromSna = "ExemptedFromSna";

    public static readonly string[] All = [CentralSectorScheme, CentrallySponsoredScheme, ExemptedFromCna, ExemptedFromSna];
}
