namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the legacy dbo.T_QEPData table (migrated raw from
/// BIMSDemo 2026-08-07 - see Others/publish-staging/migrate-sbedata-qepdata-ddgnew.sql). Source
/// for Appendix II's Q1/Q2 "Approved QEP" pre-fill, per the client's reference SQL against a prior
/// year's PrevDemandId - NOT Appendix II's own prior-year saved row (self-referential, which was
/// the pre-2026-08-07 behavior). Minimal projection - only what this query needs is mapped.
/// </summary>
public class TQepData
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public int QuarterCode { get; set; }
    public decimal Total { get; set; }
}
