namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix III-A: TSA Assignment and Expenditure. Maps to dbo.AppendixTsaAssignment (was legacy Temp_AppendixIIIA — the screenshot-confirmed example).</summary>
public class AppendixTsaAssignment : AppendixEntityBase
{
    public string EntityName { get; set; } = string.Empty;
    public decimal? BE { get; set; }
    public decimal? TsaAssignmentAsOnSept { get; set; }
    public decimal? ActualExpenditureUptoSept { get; set; }
    public decimal? UnspentAssignment { get; set; }
    public DateOnly? DateOfLastAssignment { get; set; }
    public decimal? AmountOfLastAssignment { get; set; }

    /// <summary>
    /// FRS BR-04/BR-05/BR-06 (§4.2.5): TSA Assignment cannot exceed BE; Actual Expenditure cannot
    /// exceed TSA Assignment; Amount of Last Assignment cannot exceed TSA Assignment. Then
    /// auto-calculates Unspent Assignment = TSA Assignment - Actual Expenditure. Throws
    /// ArgumentException (distinct from the repository's InvalidOperationException for the
    /// frozen-check) so the controller can keep mapping this to its original Code =
    /// "VALIDATION_ERROR" response instead of "UPDATE_FAILED".
    /// </summary>
    public void UpdateFrom(
        string entityName,
        decimal? be,
        decimal? tsaAssignmentAsOnSept,
        decimal? actualExpenditureUptoSept,
        DateOnly? dateOfLastAssignment,
        decimal? amountOfLastAssignment)
    {
        if (be is not null && tsaAssignmentAsOnSept is not null && tsaAssignmentAsOnSept > be)
        {
            throw new ArgumentException("TSA Assignment cannot exceed Budget Estimate.");
        }

        if (tsaAssignmentAsOnSept is not null && actualExpenditureUptoSept is not null && actualExpenditureUptoSept > tsaAssignmentAsOnSept)
        {
            throw new ArgumentException("Actual Expenditure cannot exceed TSA Assignment.");
        }

        if (tsaAssignmentAsOnSept is not null && amountOfLastAssignment is not null && amountOfLastAssignment > tsaAssignmentAsOnSept)
        {
            throw new ArgumentException("Amount of Last Assignment cannot exceed TSA Assignment.");
        }

        EntityName = entityName;
        BE = be;
        TsaAssignmentAsOnSept = tsaAssignmentAsOnSept;
        ActualExpenditureUptoSept = actualExpenditureUptoSept;
        // FRS BR-06: Unspent Assignment = TSA Assignment - Actual Expenditure (auto-calculated).
        UnspentAssignment = (tsaAssignmentAsOnSept ?? 0) - (actualExpenditureUptoSept ?? 0);
        DateOfLastAssignment = dateOfLastAssignment;
        AmountOfLastAssignment = amountOfLastAssignment;
    }
}
