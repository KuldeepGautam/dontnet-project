namespace UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>
/// Read-only reference data mapped onto the legacy dbo.M_SpecialScheme table (migrated raw from
/// BIMSDemo 2026-08-07 - see Others/publish-staging/migrate-specialscheme-create-viib-transtype.sql).
/// Appendix VII-B's "Special Scheme" dropdown source, scoped to StmtNo = "2" - confirmed against
/// dbo.M_SpecialStatement (SpecialStmntId=2 = "Departmental Commercial Undertakings", an exact
/// match for VII-B's own description) per the client's reference SQL. RowId is what SBEData's own
/// Spl_SchemeId column refers to.
/// </summary>
public class MSpecialScheme
{
    public int RowId { get; set; }
    public string? SplSchemeName { get; set; }
    public string? StmtNo { get; set; }
}
