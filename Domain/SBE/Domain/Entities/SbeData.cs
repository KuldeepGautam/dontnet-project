namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Part-A row: FR008's Add SBE screen. Maps dbo.SBEData — already live in UBIS-Dev with real
/// historical data (45,209 rows, FY2017-18 through FY2026-27), migrated in before this module's
/// own work started. Every measure column (Actual/BE/RE/NBE, split Plan/NonPlan) sits on THIS
/// SAME row for the FinancialYear this row was entered for — confirmed directly against BIMSDemo
/// (2026-08-25) there is no cross-year join needed to compute "Actual"; it's a sibling column,
/// not a derived lookup (see the SBE plan's "Conventions to carry over" section). BE-editability
/// (normally DDG-sourced/read-only, editable only when the matching SbeDemand.BeEditFlag is set)
/// and the ceiling "red/green" enforcement against DemandCeiling are deferred to Stage 3 — this
/// entity is a Foundation-stage data holder only.
/// </summary>
public class SbeData
{
    public int SbeDataId { get; set; }
    public string? FinancialYear { get; set; }
    public int? DemandId { get; set; }
    public int? DemandNo { get; set; }
    public int? MinistryId { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public string? SubCategoryRptSrNo { get; set; }
    public int? UmbSchemeId { get; set; }
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public string? MajorHeadCode { get; set; }

    /// <summary>'E' = Expenditure, 'R' = Recovery/Receipt — same two-value convention PreBudget's own SbeData reference entity already uses.</summary>
    public string? ExpType { get; set; }
    public string? PlanType { get; set; }

    public decimal? ActualPlan { get; set; }
    public decimal? ActualNonPlan { get; set; }
    public decimal? BePlan { get; set; }
    public decimal? BeNonPlan { get; set; }
    public decimal? RePlan { get; set; }
    public decimal? ReNonPlan { get; set; }
    public decimal? NbePlan { get; set; }
    public decimal? NbeNonPlan { get; set; }

    public string? ReceiptRecoveryMajorHeadCode { get; set; }
    public string? SpecialStatements { get; set; }
    public int? SplSchemeId { get; set; }
    public string? SplSchemeName { get; set; }
    public string? HSplSchemeName { get; set; }
    public int? SubsidyGroupId { get; set; }
    public string? SubsidyGroup { get; set; }
    public string? HSubsidyGroup { get; set; }
    public int? SplSeqNo { get; set; }
    public string? AnnexIIGroupName { get; set; }
    public string? HAnnexIIGroupName { get; set; }

    /// <summary>Self-referential lineage to this row's prior-year counterpart, same Prev-chain convention as M_Scheme.PrevSchemeId/M_Demand.PrevDemandId.</summary>
    public int? SbeDataIdPreviousYear { get; set; }
    public string? SbeDetailCode { get; set; }
    public string? Active { get; set; }
    public DateTime? EntryDate { get; set; }
    public int? LoginId { get; set; }
    public string? Stmt18Flag { get; set; }
    public string? Ip { get; set; }
}
