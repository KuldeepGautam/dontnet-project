namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>
/// Appendix II: Quarterly Expenditure Plan (QEP). Maps to dbo.AppendixQuarterlyExpenditurePlan
/// (was legacy Temp_AppendixII — confirmed the live/current table over Temp_QEP_Progress, see
/// design doc §6). Q1/Q2HasDeviation + Q1/Q2MofApprovalDetails are FRS-mandated fields confirmed
/// absent from the old table entirely, not just the designer prototype.
/// </summary>
public class AppendixQuarterlyExpenditurePlan : AppendixEntityBase
{
    public decimal? Q1ApprovedQepPrevYear { get; set; }
    public decimal? Q1ActualsPrevYear { get; set; }
    public decimal? Q1ApprovedQep { get; set; }
    public decimal? Q1Actuals { get; set; }
    public string? RemarksQ1 { get; set; }
    public bool Q1HasDeviation { get; set; }
    public string? Q1MofApprovalDetails { get; set; }

    public decimal? Q2ApprovedQepPrevYear { get; set; }
    public decimal? Q2ActualsPrevYear { get; set; }
    public decimal? Q2ApprovedQep { get; set; }
    public decimal? Q2Actuals { get; set; }
    public string? RemarksQ2 { get; set; }
    public bool Q2HasDeviation { get; set; }
    public string? Q2MofApprovalDetails { get; set; }

    /// <summary>Auto-calculated, read-only per FRS.</summary>
    public decimal TotalApprovedQep => (Q1ApprovedQep ?? 0) + (Q2ApprovedQep ?? 0);

    /// <summary>Auto-calculated, read-only per FRS.</summary>
    public decimal TotalActuals => (Q1Actuals ?? 0) + (Q2Actuals ?? 0);

    /// <summary>
    /// FRS Deviation Validations (§8.4/prototype review §11.3): if Q1/Q2HasDeviation is set, the
    /// matching Remarks field becomes mandatory. Throws ArgumentException (distinct from the
    /// repository's InvalidOperationException for the frozen-check) so the controller can keep
    /// mapping this to its original Code = "VALIDATION_ERROR" response instead of "UPDATE_FAILED".
    /// MoF Approval Details stays dropped from the UI (client instruction 2026-09-10) - it's no
    /// longer bound to anything on the drawer, but the column/parameter is kept so an already-saved
    /// value isn't silently wiped to null.
    /// </summary>
    public void UpdateFrom(
        decimal? q1ApprovedQepPrevYear,
        decimal? q1ActualsPrevYear,
        decimal? q1ApprovedQep,
        decimal? q1Actuals,
        string? remarksQ1,
        bool q1HasDeviation,
        string? q1MofApprovalDetails,
        decimal? q2ApprovedQepPrevYear,
        decimal? q2ActualsPrevYear,
        decimal? q2ApprovedQep,
        decimal? q2Actuals,
        string? remarksQ2,
        bool q2HasDeviation,
        string? q2MofApprovalDetails)
    {
        if (q1HasDeviation && string.IsNullOrWhiteSpace(remarksQ1))
        {
            throw new ArgumentException("Remarks (Q1) are required when a deviation from MEP/QEP is indicated.");
        }

        if (q2HasDeviation && string.IsNullOrWhiteSpace(remarksQ2))
        {
            throw new ArgumentException("Remarks (Q2) are required when a deviation from MEP/QEP is indicated.");
        }

        Q1ApprovedQepPrevYear = q1ApprovedQepPrevYear;
        Q1ActualsPrevYear = q1ActualsPrevYear;
        Q1ApprovedQep = q1ApprovedQep;
        Q1Actuals = q1Actuals;
        RemarksQ1 = remarksQ1;
        Q1HasDeviation = q1HasDeviation;
        Q1MofApprovalDetails = q1MofApprovalDetails;

        Q2ApprovedQepPrevYear = q2ApprovedQepPrevYear;
        Q2ActualsPrevYear = q2ActualsPrevYear;
        Q2ApprovedQep = q2ApprovedQep;
        Q2Actuals = q2Actuals;
        RemarksQ2 = remarksQ2;
        Q2HasDeviation = q2HasDeviation;
        Q2MofApprovalDetails = q2MofApprovalDetails;
    }
}
