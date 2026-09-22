namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Backs Appendix VI's "Receipt Type" drawer dropdown (client re-design, 2026-09-10 - see
/// UBIS-Pre-budget-html/Appendix6.html). New global master table dbo.M_Appendix_VI_ReceiptType
/// (not FinancialYear-versioned). The saved Appendix VI row stores ReceiptTypeId (FK) plus the
/// resolved <see cref="Name"/> in its own ReceiptType column for grid/export/search - same pattern
/// as Appendix III-B's CategoryId / CategoryType.
/// </summary>
public class AppendixViReceiptType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
