namespace UBIS.Services.Sbe.Infrastructure.Services;

/// <summary>
/// Single point of reference for how a Sub-Scheme is labelled everywhere it's shown across SBE.
/// Copied from PreBudget's own SubSchemeDisplayFormatter (client convention, applied to SBE from
/// day one per 2026-08-25 direction): "SchemeSrNo.SubSchemeSrNo (2 digits) - Sub Scheme Name",
/// e.g. "4.03 - Less-Amount met From Fund for Innovation" - SubSchemeSrNo always padded to 2
/// digits for values 0-9.
///
/// **Not stored pre-formatted anywhere**: direct BIMSDemo verification (2026-08-25) confirmed
/// BIMSDemo's own M_SubScheme.SubSchemeSrNo is a genuinely zero-padded varchar(3) ('01', '02'),
/// but the already-migrated UBIS-Dev.M_SubScheme has it typed as plain int (padding lost in
/// migration) - this formatter's ToString("00") re-derives the padding on read, it does not
/// assume storage already has it.
/// </summary>
public static class SubSchemeDisplayFormatter
{
    public static string Format(int? schemeSrNo, int? subSchemeSrNo, string subSchemeName) =>
        $"{schemeSrNo ?? 0}.{(subSchemeSrNo ?? 0).ToString("00")} - {subSchemeName}";
}
