namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-C: Details of Corpus Funds. Maps to dbo.AppendixCorpusFund (was legacy Temp_CorpusFund). AutonomousBodyId FKs to this service's own M_AutonomousBody (FR-004) — the old table already had this FK (as autonomousID), the request/approval workflow on top is the new part.</summary>
public class AppendixCorpusFund : AppendixEntityBase
{
    public int AutonomousBodyId { get; set; }
    public bool IsPublicAccount { get; set; }
    public decimal? AccumulatedBalancePrevYear { get; set; }
    public decimal? AccumulatedBalance { get; set; }
    public decimal? ActualExpenditureY1 { get; set; }
    public decimal? ActualExpenditureY2 { get; set; }
    public decimal? ActualExpenditureY3 { get; set; }
    public decimal? AllocationInBE { get; set; }
    public decimal? ExpenditureTillSept { get; set; }
    public string? ReasonForCorpusFund { get; set; }

    public void UpdateFrom(
        int autonomousBodyId,
        bool isPublicAccount,
        decimal? accumulatedBalancePrevYear,
        decimal? accumulatedBalance,
        decimal? actualExpenditureY1,
        decimal? actualExpenditureY2,
        decimal? actualExpenditureY3,
        decimal? allocationInBE,
        decimal? expenditureTillSept,
        string? reasonForCorpusFund)
    {
        AutonomousBodyId = autonomousBodyId;
        IsPublicAccount = isPublicAccount;
        AccumulatedBalancePrevYear = accumulatedBalancePrevYear;
        AccumulatedBalance = accumulatedBalance;
        ActualExpenditureY1 = actualExpenditureY1;
        ActualExpenditureY2 = actualExpenditureY2;
        ActualExpenditureY3 = actualExpenditureY3;
        AllocationInBE = allocationInBE;
        ExpenditureTillSept = expenditureTillSept;
        ReasonForCorpusFund = reasonForCorpusFund;
    }
}
