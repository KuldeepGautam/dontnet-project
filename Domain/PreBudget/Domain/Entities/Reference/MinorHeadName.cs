namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto dbo.M_MinorHeadName (migrated 2026-09-09 from BIMSDemo's
/// dbo.Actdrx, LEN(AccountHead)=9 rows only - see
/// Others/publish-staging/prebudget-workstream-migrate-minorheadname.sql). Appendix VI-F's "Minor
/// Head" name lookup, per the client's own reference SQL: "select * from Actdrx where
/// Financialyear=@fy and AccountHead=@MinorHead".
/// </summary>
public class MinorHeadName
{
    public int Id { get; set; }
    public string AccountHead { get; set; } = string.Empty;
    public string? AccountHeadName { get; set; }
    public string? HAccountHeadName { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
}
