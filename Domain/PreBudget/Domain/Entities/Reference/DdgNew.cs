namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the legacy dbo.DDG table (migrated raw from BIMSDemo
/// 2026-08-07 as DDGNew, renamed to DDG 2026-08-10 - see
/// Others/publish-staging/migrate-sbedata-qepdata-ddgnew.sql and rename-ddgnew-to-ddg.sql). Source for
/// Appendix V-B's "load BE on Object Head change" pre-fill, per the client's reference SQL against
/// a prior year's PrevDemandId, matching the Object Head by the last 2 characters of HeadOfAccount
/// and excluding rows with no SchemeId. NOT filtered by FinancialYear (the client's reference query
/// doesn't filter on it either - DemandId alone is year-specific via the PrevDemandId chain).
/// Minimal projection - only what this query needs is mapped.
/// </summary>
public class DdgNew
{
    public int ComputerSlno { get; set; }
    public int DemandId { get; set; }
    public string HeadOfAccount { get; set; } = string.Empty;
    public decimal NBE_Plan { get; set; }
    public decimal Actual_Plan { get; set; }
    public int? SchemeId { get; set; }

    // Added 2026-09-09 for Appendix VI-F's Minor Head autocomplete (client's own reference SQL:
    // "select distinct left(HeadOfAccount,9) ... where FinancialYear=@fy and Demandno=@demandNo") -
    // both columns already exist on the underlying dbo.DDG table, just weren't mapped by this
    // "minimal projection" entity yet. Purely additive to this class; V-B's existing query (the only
    // other consumer) is unaffected.
    public string FinancialYear { get; set; } = string.Empty;
    public int? DemandNo { get; set; }
}
