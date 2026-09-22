namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Backs Appendix VII-B's "Transaction Type" dropdown (client requirement, 2026-08-07 - new table,
/// not migrated from BIMSDemo). Semantically mirrors legacy SBEData.Exp_Type ('E'=Expenditure,
/// 'R'=Recovery), but AppendixCommercialUndertakingReceipts.TransactionType stores the full label
/// directly (no FK) - this table only backs the dropdown's option list.
/// </summary>
public class AppendixViibTransactionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
