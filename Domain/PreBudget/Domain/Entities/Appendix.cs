namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_Appendix table — replaces legacy BIMSDemo.M_TempControl. Year-scoped with
/// a PrevAppendixId carry-forward chain (decision 2, 2026-07-23), matching M_Demand/M_User's
/// existing pattern elsewhere in UBIS-Dev.
/// </summary>
public class Appendix
{
    public int AppendixId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>E.g. "I", "I-A", "II", "III-A", "VI-C", "PA-ReceiptPayment".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? HName { get; set; }

    /// <summary>Free-text note shown on the appendix's own entry screen (client requirement
    /// 2026-09-17, piloted on Appendix I first) - set directly in dbo.M_Appendix, not editable
    /// from any UBIS_Web screen yet.</summary>
    public string? Remarks { get; set; }

    /// <summary>Government circular paragraph reference (e.g. "1.2") shown as "(See Para {ParaNo})"
    /// on the appendix's export only, not the on-screen entry page (client requirement 2026-09-17)
    /// - set directly in dbo.M_Appendix, not editable from any UBIS_Web screen yet.</summary>
    public string? ParaNo { get; set; }

    public int DisplaySequence { get; set; }

    public int? PrevAppendixId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
