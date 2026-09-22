namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the legacy dbo.SBEData table (migrated raw from BIMSDemo
/// 2026-08-07 - see Others/publish-staging/migrate-sbedata-qepdata-ddgnew.sql and
/// migrate-specialscheme-create-viib-transtype.sql). Unlike SbeNbeSummary/SbeNbeSummaryByYear
/// (pre-aggregated, no MajorHeadCode), this is the raw source needed for:
/// - Appendix I-A's Revenue (MajorHeadCode &lt; 4000) vs Capital (&gt;= 4000) BE split.
/// - Appendix VII-B's Major-Head/Transaction-Type-scoped BE (NBE_PLan) and Actuals (Actual_plan),
///   filtered by Spl_SchemeId and Exp_Type ('E'=Expenditure, 'R'=Recovery).
/// Both keyed off a prior year's PrevDemandId, per the client's reference SQL. Minimal projection -
/// SBEData has ~40 columns, only what these queries need is mapped.
/// </summary>
public class SbeData
{
    public int SBEDataID { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string MajorHeadCode { get; set; } = string.Empty;
    public decimal NBE_PLan { get; set; }
    public decimal Actual_plan { get; set; }
    public int? Spl_SchemeId { get; set; }
    public string? Exp_Type { get; set; }

    /// <summary>Appendix VI-B's BE (client reference SQL, 2026-08-25): "select sum(BE_Plan) from
    /// SBEData where DemandId=... and CategoryId=... and SchemeID=...". SchemeID here is the plain
    /// M_Scheme id (not Spl_SchemeId, which is the Special Scheme used by VII-A/VII-B/PA).</summary>
    public int? CategoryId { get; set; }
    public int? SchemeID { get; set; }
    public decimal BE_Plan { get; set; }
}
