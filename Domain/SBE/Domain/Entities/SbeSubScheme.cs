namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Maps dbo.M_SubScheme (shared with ECL/PreBudget). SubSchemeSrNo is typed int here — the
/// legacy BIMSDemo source stores it as a zero-padded varchar(3) ('01','02'), lost in migration
/// (confirmed 2026-08-25) — SubSchemeDisplayFormatter re-derives the padded 2-digit display from
/// this int, don't assume storage already has it.
/// </summary>
public class SbeSubScheme
{
    public int SubSchemeId { get; set; }
    public int SchemeId { get; set; }
    public string SubSchemeName { get; set; } = string.Empty;
    public string? HSubSchemeName { get; set; }
    public string? SubSchemeCode { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int? PrevSubschemeId { get; set; }
    public int? SubSchemeSrNo { get; set; }
}
