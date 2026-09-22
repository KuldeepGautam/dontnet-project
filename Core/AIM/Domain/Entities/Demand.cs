namespace UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps to the real, DBA-owned <c>dbo.M_Demand</c> table (44 columns total — only the subset
/// needed for demand-selection dropdowns is mapped here; unmapped columns are simply ignored by EF
/// Core, not deleted or affected). Year-scoped, unlike <see cref="MapUserRole"/> (role no longer
/// varies by year as of the 2026-08-04 consolidation).
/// Added 2026-07-23 to back <c>GET /api/users/demands</c> — until now, the accessible Demand IDs
/// only ever existed as boolean JWT claims (<c>AIM:Demand:{demandId}</c>), never as a queryable
/// list with names.
/// </summary>
public class Demand
{
    public int DemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public int DemandNo { get; set; }

    public string DemandName { get; set; } = string.Empty;

    public string? HDemandName { get; set; }

    /// <summary>Confirmed NULL for every real row in the live dataset (never populated) — nullable
    /// here to match, and deliberately not used as a query filter anywhere (see AuthenticationService
    /// .ParseDemandIdsAsync and UserLookupController.GetMyDemands, both fixed 2026-07-23 after this
    /// silently excluded every demand).</summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 'E'/'R'/'Y' classification, promoted onto the DemandNo-keyed <c>dbo.M_Demand</c> master
    /// (2026-08-28, client requirement) from <c>dbo.M_MapDemandFY.DemandTypeFlag</c>'s latest-FY
    /// value — year-independent by design, unlike the per-year <c>DemandTypeFlag</c> it was copied
    /// from (confirmed live: 26 of 106 DemandNos had inconsistent values across their FY history;
    /// the client's instruction was to standardize on the latest FY, 2026-2027, as the single
    /// source of truth going forward). <see cref="UserLookupController.GetMyDemands"/> filters on
    /// DemandType == "E" — the single shared "which Demands can this user pick from" query every
    /// module's Demand-selection screen (PreBudget's Appendix main screen, Allocation screen, ECL)
    /// calls, so this one filter point covers all of them.
    /// </summary>
    public string? DemandType { get; set; }
}
