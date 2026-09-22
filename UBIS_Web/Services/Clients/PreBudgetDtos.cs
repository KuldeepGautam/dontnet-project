namespace UBIS.Web.Services.Clients;

// -----------------------------------------------------------------------------------------------
// Mirrors Domain\PreBudget\Application\DTOs\* exactly — field names/shapes must match the real
// PreBudget microservice responses. Added 2026-07-23 for the Pre-Budget Meeting module's UI.
// -----------------------------------------------------------------------------------------------

public class PreBudgetAppendixDto
{
    public int AppendixId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
    public int DisplaySequence { get; set; }
}

/// <summary>Per-Demand status list — the appendix-selector dropdown uses this.</summary>
public class PreBudgetAppendixWithStatusDto
{
    public int AppendixId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ParaNo { get; set; }
    public int DisplaySequence { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool IsTargetDateExpired { get; set; }
}

public class SetNilSubmissionDto
{
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? NilRemarks { get; set; }
}

// --- Add Allocation screen -----------------------------------------------------------------------

public class NotificationChannelFlags
{
    public bool Email { get; set; }
    public bool Sms { get; set; }
}

public class AllocateAppendixDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
    public int? AppendixId { get; set; }
    public List<int> DemandIds { get; set; } = new();

    public NotificationChannelFlags NotifyDemandBudgetOfficer { get; set; } = new();
    public NotificationChannelFlags NotifyDemandCca { get; set; } = new();
    public NotificationChannelFlags NotifyDemandFa { get; set; } = new();
    public NotificationChannelFlags NotifyBudgetDivisionOfficer { get; set; } = new();
    public NotificationChannelFlags NotifySectionUser { get; set; } = new();
}

public class AllocationResultDto
{
    public int DemandsAllocated { get; set; }
    public int AppendixesAllocated { get; set; }
    public List<int> SkippedFrozenDemandIds { get; set; } = new();
}

public class AllocationListItemDto
{
    public int DemandId { get; set; }
    public int DemandNo { get; set; }

    /// <summary>Added 2026-08-28 (grid shows "Demand Name" - "{DemandNo} - {DemandName}" - instead of the bare Demand No).</summary>
    public string DemandName { get; set; } = string.Empty;

    public DateTime? TargetDate { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
}

/// <summary>Row-level edit for the existing-allocations grid - see the PreBudget microservice's own UpdateAllocationDto doc comment. Added 2026-08-28.</summary>
public class UpdateAllocationDto
{
    public int DemandId { get; set; }
    public int? AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
}

// --- Appendix I: Budget and Expenditure Trends -------------------------------------------------

public class AppendixBudgetExpenditureTrendDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixBudgetExpenditureTrendDto
{
    // Not on the WebApi's own Save DTO (Update routes by URL id, not body) - client-local only,
    // used by SaveAppendixI to decide Create vs Update, same convention as I-A/II's own Save*Dto.
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? RevenueBE { get; set; }
    public decimal? RevenueRE { get; set; }
    public decimal? RevenueActuals { get; set; }
    public decimal? RevenueActualsUptoSept { get; set; }
    public decimal? CapitalBE { get; set; }
    public decimal? CapitalRE { get; set; }
    public decimal? CapitalActuals { get; set; }
    public decimal? CapitalActualsUptoSept { get; set; }
}

// --- Appendix II: Quarterly Expenditure Plan (QEP) ----------------------------------------------

public class AppendixQuarterlyExpenditurePlanDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    public decimal? Q1ApprovedQepPrevYear { get; set; }
    public decimal? Q1ActualsPrevYear { get; set; }
    public decimal? Q1ApprovedQep { get; set; }
    public decimal? Q1Actuals { get; set; }
    public string? RemarksQ1 { get; set; }
    public bool Q1HasDeviation { get; set; }
    public string? Q1MofApprovalDetails { get; set; }

    public decimal? Q2ApprovedQepPrevYear { get; set; }
    public decimal? Q2ActualsPrevYear { get; set; }
    public decimal? Q2ApprovedQep { get; set; }
    public decimal? Q2Actuals { get; set; }
    public string? RemarksQ2 { get; set; }
    public bool Q2HasDeviation { get; set; }
    public string? Q2MofApprovalDetails { get; set; }

    public decimal TotalApprovedQep { get; set; }
    public decimal TotalActuals { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixQuarterlyExpenditurePlanDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;

    public decimal? Q1ApprovedQepPrevYear { get; set; }
    public decimal? Q1ActualsPrevYear { get; set; }
    public decimal? Q1ApprovedQep { get; set; }
    public decimal? Q1Actuals { get; set; }
    public string? RemarksQ1 { get; set; }
    public bool Q1HasDeviation { get; set; }
    public string? Q1MofApprovalDetails { get; set; }

    public decimal? Q2ApprovedQepPrevYear { get; set; }
    public decimal? Q2ActualsPrevYear { get; set; }
    public decimal? Q2ApprovedQep { get; set; }
    public decimal? Q2Actuals { get; set; }
    public string? RemarksQ2 { get; set; }
    public bool Q2HasDeviation { get; set; }
    public string? Q2MofApprovalDetails { get; set; }
}

// --- Appendix III-A: TSA Assignment and Expenditure ---------------------------------------------

public class AppendixTsaAssignmentDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal? BE { get; set; }
    public decimal? TsaAssignmentAsOnSept { get; set; }
    public decimal? ActualExpenditureUptoSept { get; set; }
    public decimal? UnspentAssignment { get; set; }
    public DateOnly? DateOfLastAssignment { get; set; }
    public decimal? AmountOfLastAssignment { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixTsaAssignmentDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public decimal? BE { get; set; }
    public decimal? TsaAssignmentAsOnSept { get; set; }
    public decimal? ActualExpenditureUptoSept { get; set; }
    public DateOnly? DateOfLastAssignment { get; set; }
    public decimal? AmountOfLastAssignment { get; set; }
}

// --- FR-003: RE Data Remarks ---------------------------------------------------------------------

public class PreBudgetRemarkDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RemarkText { get; set; } = string.Empty;
    public string? CreatedByRoleSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreatePreBudgetRemarkDto
{
    public int DemandId { get; set; }
    public int AppendixId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RemarkText { get; set; } = string.Empty;
}

// --- FR-004: Autonomous Master --------------------------------------------------------------------

public class AutonomousBodyDto
{
    public int AutonomousBodyId { get; set; }
    public int DemandId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
}

/// <summary>Administrator-only direct add - matches the legacy "Add Autonomous/Grantee Name" form.</summary>
public class CreateAutonomousBodyDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
}

public class CreateAutonomousBodyRequestDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RequestedName { get; set; } = string.Empty;
}

public class AutonomousBodyRequestDto
{
    public int RequestId { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string RequestedName { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
}

public class ReviewAutonomousBodyRequestDto
{
    public bool Approve { get; set; }
    public string? ReviewRemarks { get; set; }
    public string? Code { get; set; }
}

// -----------------------------------------------------------------------------------------------
// The remaining 19 appendices — added 2026-07-23. Mirrors Domain\PreBudget\Application\DTOs\
// Appendices\* exactly.
// -----------------------------------------------------------------------------------------------

// --- Appendix I-A: Projected Demand by Ministry ---
public class AppendixProjectedDemandDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? PrevYrMinRevenueBE { get; set; }
    public decimal? PrevYrMinRevenueRE { get; set; }
    public decimal? PrevYrMinCapitalBE { get; set; }
    public decimal? PrevYrMinCapitalRE { get; set; }
    public decimal? CurrYrMinRevenueBE { get; set; }
    public decimal? CurrYrMinCapitalBE { get; set; }
    public decimal? CurrYrMinMtefRevenueBE { get; set; }
    public decimal? CurrYrMinMtefCapitalBE { get; set; }
    public decimal TotalBE { get; set; }
    public decimal PrevYrTotalBE { get; set; }
    public decimal PrevYrTotalRE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixProjectedDemandDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? PrevYrMinRevenueBE { get; set; }
    public decimal? PrevYrMinRevenueRE { get; set; }
    public decimal? PrevYrMinCapitalBE { get; set; }
    public decimal? PrevYrMinCapitalRE { get; set; }
    public decimal? CurrYrMinRevenueBE { get; set; }
    public decimal? CurrYrMinCapitalBE { get; set; }
    public decimal? CurrYrMinMtefRevenueBE { get; set; }
    public decimal? CurrYrMinMtefCapitalBE { get; set; }
}

// --- Appendix III: CNA/SNA Balances of Schemes ---
public class AppendixCnaSnaBalanceDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Added 2026-09-21 for the structured Scheme->Sub-Scheme export - same fields/
    /// reasoning as AppendixEstimatesOfSchemesDto (Appendix IV)'s own copies.</summary>
    public int? SchemeSrNo { get; set; }
    public string? SchemeNameRaw { get; set; }
    public string? SubSchemeNameRaw { get; set; }
    public int? SubSchemeSrNo { get; set; }

    public decimal? BE { get; set; }
    public decimal? BalanceAsOnAprilOpening { get; set; }
    public decimal? ReleasesDuringFY { get; set; }
    public decimal? BalanceAsOnSeptClosing { get; set; }
    public DateOnly? DateOfLastRelease { get; set; }
    public decimal? AmountOfLastRelease { get; set; }
    public decimal? NotTransferredToSnaAsOnSept { get; set; }
    public string? Remarks { get; set; }
    public string? ReasonForExemption { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixCnaSnaBalanceDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? BE { get; set; }
    public decimal? BalanceAsOnAprilOpening { get; set; }
    public decimal? ReleasesDuringFY { get; set; }
    public decimal? BalanceAsOnSeptClosing { get; set; }
    public DateOnly? DateOfLastRelease { get; set; }
    public decimal? AmountOfLastRelease { get; set; }
    public decimal? NotTransferredToSnaAsOnSept { get; set; }
    public string? Remarks { get; set; }
    public string? ReasonForExemption { get; set; }
}

// --- Appendix III: reference lookups for the Balance Type -> Scheme -> SubScheme cascade ---
public class AppendixIIICategoryOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

/// <summary>Response shape for the BE auto-load endpoints (SUM(NBE_plan) from SBEData for a Scheme/SubScheme + FY) - Appendix III, IV, IV-A, IV-B.</summary>
public class AppendixBeAutoLoadDto
{
    public decimal Be { get; set; }
}

/// <summary>Appendix V-B's BE and Actuals pre-fill on Object Head change, resolved via M_Demand.PrevDemandId + dbo.DDG (client reference SQL, 2026-08-07 BE / 2026-08-10 Actuals).</summary>
public class AppendixVBBeActualsDto
{
    public decimal Be { get; set; }
    public decimal Actuals { get; set; }
}

/// <summary>Appendix I-A's previous-year Revenue/Capital BE, resolved via M_Demand.PrevDemandId + dbo.SBEData (client reference SQL, 2026-08-07).</summary>
public class AppendixIAPreviousYearBeDto
{
    public bool Found { get; set; }
    public decimal RevenueBE { get; set; }
    public decimal CapitalBE { get; set; }
}

/// <summary>Appendix II's previous-year "As per approved QEP" Q1/Q2, resolved via M_Demand.PrevDemandId + dbo.T_QEPData (client reference SQL, 2026-08-07).</summary>
public class AppendixIIPreviousYearApprovedQepDto
{
    public bool Found { get; set; }
    public decimal Q1ApprovedQep { get; set; }
    public decimal Q2ApprovedQep { get; set; }
}

/// <summary>Appendix VII-A's Major Head dropdown option, scoped to the selected Demand (client review 2026-08-05).</summary>
public class AppendixVIIAMajorHeadOptionDto
{
    public int MajorHeadId { get; set; }
    public string? MajorHeadName { get; set; }
}

public class AppendixIIISchemeOptionDto
{
    public int SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
}

public class AppendixIIISubSchemeOptionDto
{
    public int SubSchemeId { get; set; }
    public string SubSchemeName { get; set; } = string.Empty;
}

// --- Appendix IV: Estimates of Schemes ---
public class AppendixEstimatesOfSchemesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Scheme's own Category name (via M_Scheme.CategoryId) - added 2026-09-18 for the
    /// export's circular-mandated Centrally Sponsored Schemes/Central Sector Schemes grouping.
    /// Null when the Scheme has no CategoryId set.</summary>
    public string? CategoryType { get; set; }

    /// <summary>M_Category.SerialNo ("I".."VI") for CategoryType above - added 2026-09-21 for the
    /// Budget Division export's Category ordering.</summary>
    public string? CategorySerialNo { get; set; }

    /// <summary>Raw M_Scheme.SchemeSrNo - added 2026-09-21 for the Budget Division export's Scheme
    /// grouping/ordering.</summary>
    public int? SchemeSrNo { get; set; }

    /// <summary>Plain names (no "N - "/"N.NN - " prefix) - added 2026-09-21 so the Budget Division
    /// export can build its own "N. Name"/"N.NN- Name" row labels (client-supplied reference file),
    /// distinct from SchemeName/SubSchemeName's shared grid/dropdown format above.</summary>
    public string? SchemeNameRaw { get; set; }

    public string? SubSchemeNameRaw { get; set; }

    public int? SubSchemeSrNo { get; set; }

    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? AddlReSought { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? AddlNbeSought { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }
    public decimal? PercentWrtBE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixEstimatesOfSchemesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>Budget Division/ABO-DS-Director/Section User-entered, gated to those roles on the
    /// edit drawer (client instruction 2026-09-21).</summary>
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
}

// --- Appendix IV-A: SCSP Expenditure ---
public class AppendixScspExpenditureDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Added 2026-09-21 for the structured Single Demand User/Budget Division export -
    /// same fields/reasoning as AppendixEstimatesOfSchemesDto (Appendix IV)'s own copies.</summary>
    public string? CategoryType { get; set; }
    public string? CategorySerialNo { get; set; }
    public int? SchemeSrNo { get; set; }
    public string? SchemeNameRaw { get; set; }
    public string? SubSchemeNameRaw { get; set; }
    public int? SubSchemeSrNo { get; set; }

    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? AddlReSought { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? AddlNbeSought { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixScspExpenditureDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>Budget Division/ABO-DS-Director/Section User-entered, gated to those roles on the
    /// edit drawer (client instruction 2026-09-21).</summary>
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
}

// --- Appendix IV-B: TASP Expenditure (same shape as IV-A) ---
public class AppendixTaspExpenditureDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }

    /// <summary>Added 2026-09-21 for the structured Single Demand User/Budget Division export -
    /// same fields/reasoning as AppendixEstimatesOfSchemesDto (Appendix IV)'s own copies.</summary>
    public string? SchemeNameRaw { get; set; }
    public int? SchemeSrNo { get; set; }
    public string? SubSchemeNameRaw { get; set; }
    public int? SubSchemeSrNo { get; set; }

    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? AddlReSought { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? AddlNbeSought { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixTaspExpenditureDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? RemarksMinistry { get; set; }
    public string? RemarksBudget { get; set; }

    /// <summary>Budget Division/ABO-DS-Director/Section User-entered, gated to those roles on the
    /// edit drawer (client instruction 2026-09-21).</summary>
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
}

// --- Appendix V: Estimates of Establishment & Other Central Expenditure ---
public class AppendixEstablishmentExpenditureDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? BudgetRecommendedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public decimal? BudgetRecommendedNBE { get; set; }
    public string? RemarksBudget { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixEstablishmentExpenditureDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? RemarksBudget { get; set; }
}

// --- Appendix V-A: Grant in Aid to Autonomous and Other Bodies ---
public class AppendixGrantInAidDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public decimal? GiaGeneralActuals { get; set; }
    public decimal? GiaGeneralActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaGeneralBE { get; set; }
    public decimal? GiaGeneralActualsUptoSept { get; set; }
    public decimal? GiaGeneralRE { get; set; }
    public decimal? GiaGeneralNBE { get; set; }
    public decimal? GiaCcaActuals { get; set; }
    public decimal? GiaCcaActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaCcaBE { get; set; }
    public decimal? GiaCcaActualsUptoSept { get; set; }
    public decimal? GiaCcaRE { get; set; }
    public decimal? GiaCcaNBE { get; set; }
    public decimal? GiaSalaryActuals { get; set; }
    public decimal? GiaSalaryActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaSalaryTotal { get; set; }
    public decimal? GiaSalaryBE { get; set; }
    public decimal? GiaSalaryActualsUptoSept { get; set; }
    public decimal? GiaSalaryRE { get; set; }
    public decimal? GiaSalaryNBE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixGrantInAidDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public decimal? GiaGeneralActuals { get; set; }
    public decimal? GiaGeneralActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaGeneralBE { get; set; }
    public decimal? GiaGeneralActualsUptoSept { get; set; }
    public decimal? GiaGeneralRE { get; set; }
    public decimal? GiaGeneralNBE { get; set; }
    public decimal? GiaCcaActuals { get; set; }
    public decimal? GiaCcaActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaCcaBE { get; set; }
    public decimal? GiaCcaActualsUptoSept { get; set; }
    public decimal? GiaCcaRE { get; set; }
    public decimal? GiaCcaNBE { get; set; }
    public decimal? GiaSalaryActuals { get; set; }
    public decimal? GiaSalaryActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaSalaryTotal { get; set; }
    public decimal? GiaSalaryBE { get; set; }
    public decimal? GiaSalaryActualsUptoSept { get; set; }
    public decimal? GiaSalaryRE { get; set; }
    public decimal? GiaSalaryNBE { get; set; }
}

// --- Appendix V-B: Establishment Expenditure - Object Head wise ---
public class AppendixEstablishmentByObjectHeadDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ObjectHeadId { get; set; }
    public string? ObjectHeadCode { get; set; }
    public string? ObjectHeadName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixEstablishmentByObjectHeadDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ObjectHeadId { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
}

public class AppendixVBObjectHeadOptionDto
{
    public int ObjectHeadId { get; set; }
    public string Label { get; set; } = string.Empty;
}

// --- Appendix V-C: Establishment Expenditure - Other than AB ---
public class AppendixEstablishmentOtherThanABDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixEstablishmentOtherThanABDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Actuals { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedRE { get; set; }
    public decimal? ProposedNBE { get; set; }
    public string? Remarks { get; set; }
}

// --- Appendix VI: Non-Tax Revenue ---
public class AppendixNonTaxRevenueDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ReceiptTypeId { get; set; }
    public string? ReceiptType { get; set; }
    public string? PsuReceiptName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedBE { get; set; }
    public decimal? ProposedCollectionQ3 { get; set; }
    public decimal? ProposedCollectionQ4 { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixNonTaxRevenueDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? ReceiptTypeId { get; set; }
    public string? ReceiptType { get; set; }
    public string? PsuReceiptName { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ProposedBE { get; set; }
    public decimal? ProposedCollectionQ3 { get; set; }
    public decimal? ProposedCollectionQ4 { get; set; }
    public string? Remarks { get; set; }
}

public class AppendixVIReceiptTypeOptionDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// --- Appendix VI-A: List of User Charges ---
public class AppendixUserChargesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? TitleOfCharge { get; set; }
    public string? Service { get; set; }
    public string? OrgDept { get; set; }
    public string? RateOfCharge { get; set; }
    public string? UnitOfCollection { get; set; }
    public DateTime? DateOfRateFixation { get; set; }
    public string? FixationStatute { get; set; }
    public decimal? TotalRevenueY1 { get; set; }
    public decimal? TotalRevenueY2 { get; set; }
    public decimal? TotalRevenueY3 { get; set; }
    public string? CompetentAuthority { get; set; }
    public string? PeriodOfFixation { get; set; }
    public decimal? Salary { get; set; }
    public decimal? OfficeExpenses { get; set; }
    public decimal? OtherExpenses { get; set; }
    public bool IsCollectionCostHigher { get; set; }
    public bool IsTransCostHigher { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixUserChargesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? TitleOfCharge { get; set; }
    public string? Service { get; set; }
    public string? OrgDept { get; set; }
    public string? RateOfCharge { get; set; }
    public string? UnitOfCollection { get; set; }
    public DateTime? DateOfRateFixation { get; set; }
    public string? FixationStatute { get; set; }
    public decimal? TotalRevenueY1 { get; set; }
    public decimal? TotalRevenueY2 { get; set; }
    public decimal? TotalRevenueY3 { get; set; }
    public string? CompetentAuthority { get; set; }
    public string? PeriodOfFixation { get; set; }
    public decimal? Salary { get; set; }
    public decimal? OfficeExpenses { get; set; }
    public decimal? OtherExpenses { get; set; }
    public bool IsCollectionCostHigher { get; set; }
    public bool IsTransCostHigher { get; set; }
    public string? Remarks { get; set; }
}

// --- Appendix VI-B: Pending Liabilities ---
public class AppendixPendingLiabilitiesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public int? SubSchemeId { get; set; }
    public string? SubSchemeName { get; set; }
    public decimal? PendingLiabilityAsOnMarch31 { get; set; }
    public decimal? BE { get; set; }
    public decimal? EstimatedExpenditure { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixPendingLiabilitiesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public decimal? PendingLiabilityAsOnMarch31 { get; set; }
    public decimal? BE { get; set; }
    public decimal? EstimatedExpenditure { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Category dropdown option for Appendix VI-B's Category -> Scheme -> SubScheme cascade (added 2026-07-30).</summary>
public class AppendixVIBCategoryOptionDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

// --- Appendix VI-C: Details of Corpus Funds ---
public class AppendixCorpusFundDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public bool IsPublicAccount { get; set; }
    public decimal? AccumulatedBalancePrevYear { get; set; }
    public decimal? AccumulatedBalance { get; set; }
    public decimal? ActualExpenditureY1 { get; set; }
    public decimal? ActualExpenditureY2 { get; set; }
    public decimal? ActualExpenditureY3 { get; set; }
    public decimal? AllocationInBE { get; set; }
    public decimal? ExpenditureTillSept { get; set; }
    public string? ReasonForCorpusFund { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixCorpusFundDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public bool IsPublicAccount { get; set; }
    public decimal? AccumulatedBalancePrevYear { get; set; }
    public decimal? AccumulatedBalance { get; set; }
    public decimal? ActualExpenditureY1 { get; set; }
    public decimal? ActualExpenditureY2 { get; set; }
    public decimal? ActualExpenditureY3 { get; set; }
    public decimal? AllocationInBE { get; set; }
    public decimal? ExpenditureTillSept { get; set; }
    public string? ReasonForCorpusFund { get; set; }
}

// --- Appendix VI-D: Available internal resources ---
public class AppendixInternalResourcesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public decimal? AsOnMarch31 { get; set; }
    public decimal? AsOnJune30 { get; set; }
    public decimal? ExpectedNextMarch31 { get; set; }
    public decimal? ExpectedNextFY { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixInternalResourcesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? AutonomousBodyId { get; set; }
    public decimal? AsOnMarch31 { get; set; }
    public decimal? AsOnJune30 { get; set; }
    public decimal? ExpectedNextMarch31 { get; set; }
    public decimal? ExpectedNextFY { get; set; }
    public string? Remarks { get; set; }
}

// --- Appendix VI-E: Corpus Fund ABGiA ---
public class AppendixCorpusFundAbGiaDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public decimal? CorpusFundBalance1 { get; set; }
    public decimal? CorpusFundBalance2 { get; set; }
    public string? CorpusFundBankName { get; set; }
    public string? CorpusFundReason { get; set; }
    public decimal? GiaRE { get; set; }
    public decimal? GiaBE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixCorpusFundAbGiaDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public decimal? CorpusFundBalance1 { get; set; }
    public decimal? CorpusFundBalance2 { get; set; }
    public string? CorpusFundBankName { get; set; }
    public string? CorpusFundReason { get; set; }
    public decimal? GiaRE { get; set; }
    public decimal? GiaBE { get; set; }
}

// --- Appendix VI-F: User Charges of Ministries/Departments (Minor Head) ---
public class AppendixMinorHeadUserChargesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string MinorHeadCode { get; set; } = string.Empty;
    public string? MinorHeadName { get; set; }
    public string? BriefOnReceipts { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? NoOfTransactions { get; set; }
    public string? RateOfService { get; set; }
    public decimal? ReceiptsCollection { get; set; }
    public string? ActionTakenPlan { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixMinorHeadUserChargesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string MinorHeadCode { get; set; } = string.Empty;
    public string? BriefOnReceipts { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? NoOfTransactions { get; set; }
    public string? RateOfService { get; set; }
    public decimal? ReceiptsCollection { get; set; }
    public string? ActionTakenPlan { get; set; }
}

public class MinorHeadSuggestionDto
{
    public string Code { get; set; } = string.Empty;
}

public class MinorHeadNameDto
{
    public bool Found { get; set; }
    public string? Name { get; set; }
}

// --- Appendix VI-G: User Charges of Autonomous Bodies / various organizations ---
public class AppendixUserChargesAutonomousBodyDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? AutonomousBodyName { get; set; }
    public string? BriefOnRevenueSources { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? ReceiptsCollected { get; set; }
    public decimal? TotalRevenueExpenditure { get; set; }
    public decimal? TotalCapitalExpenditure { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixUserChargesAutonomousBodyDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }
    public string? BriefOnRevenueSources { get; set; }
    public string? PresentStatus { get; set; }
    public decimal? ReceiptsCollected { get; set; }
    public decimal? TotalRevenueExpenditure { get; set; }
    public decimal? TotalCapitalExpenditure { get; set; }
}

// --- Appendix VII-A: Recoveries ---
public class AppendixRecoveriesDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SchemeName { get; set; }
    public int? MajorHeadId { get; set; }
    public string? MajorHeadName { get; set; }
    public bool IsCharged { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixRecoveriesDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SchemeName { get; set; }
    public int? MajorHeadId { get; set; }
    public bool IsCharged { get; set; }
    public decimal? Actuals { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
}

// --- Appendix VII-B: Commercial Undertaking Receipts ---
public class AppendixCommercialUndertakingReceiptsDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public string? TransactionType { get; set; }
    public int? MajorHeadId { get; set; }
    public string? MajorHeadCode { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? IncreasedBE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

/// <summary>Special Scheme dropdown option (dbo.M_SpecialScheme, StmtNo="2" - "Departmental Commercial Undertakings").</summary>
public class AppendixVIIBSchemeOptionDto
{
    public int SchemeId { get; set; }
    public string? SchemeName { get; set; }
}

/// <summary>Transaction Type dropdown option (dbo.Appendix_VIIB_Transaction_Type).</summary>
public class AppendixVIIBTransactionTypeOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Major Head dropdown option, cascading from the selected Scheme.</summary>
public class AppendixVIIBMajorHeadOptionDto
{
    public int MajorHeadId { get; set; }
    public string? MajorHeadCode { get; set; }

    // Bug report 2026-08-27: dropdown showed "undefined" for every row. Root cause - this property
    // was named MajorHeadName, but AppendixVIIBController.GetMajorHeads actually returns a field
    // named "majorHeadLabel" (see its anonymous-type response), not "majorHeadName". System.Text.Json
    // deserialization silently leaves an unmatched property at its default (null) instead of
    // throwing, so this went unnoticed until Major Head started returning real, non-empty rows -
    // PreBudgetClient never threw, the WebApi's own JSON was always correct (confirmed live via a
    // direct call), only this DTO's re-serialization back to the browser was ever missing the label.
    // Renamed to match the WebApi's actual field name exactly.
    public string? MajorHeadLabel { get; set; }
}

/// <summary>Previous-year BE/Actuals via M_Demand.PrevDemandId + dbo.SBEData, scoped by Scheme/Major Head/Transaction Type (client reference SQL, 2026-08-07).</summary>
public class AppendixVIIBBeActualsDto
{
    public decimal Be { get; set; }
    public decimal Actuals { get; set; }
}

public class SaveAppendixCommercialUndertakingReceiptsDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? SchemeId { get; set; }
    public string? TransactionType { get; set; }
    public int? MajorHeadId { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? ActualsUptoSeptPrevYear { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? IncreasedBE { get; set; }
    public decimal? NBE { get; set; }
}

// --- Appendix X: Loans to Government Servants ---
public class AppendixLoansToGovtServantsDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SubHeadName { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY3 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixLoansToGovtServantsDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string? SubHeadName { get; set; }
    public decimal? ActualsY1 { get; set; }
    public decimal? ActualsY2 { get; set; }
    public decimal? ActualsY3 { get; set; }
    public decimal? ActualsUptoSept { get; set; }
    public decimal? BE { get; set; }
    public decimal? RE { get; set; }
    public decimal? NBE { get; set; }
}

// --- Public Account Template ---
public class PublicAccountReceiptPaymentDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? MajorHeadId { get; set; }
    public decimal? ActualReceipt { get; set; }
    public decimal? ActualPayment { get; set; }
    public decimal? BalanceAtEndReceipt { get; set; }
    public decimal? BalanceAtEndPayment { get; set; }
    public decimal? BEReceipt { get; set; }
    public decimal? BEPayment { get; set; }
    public decimal? AdjustmentReceipt { get; set; }
    public decimal? AdjustmentPayment { get; set; }
    public decimal? REReceipt { get; set; }
    public decimal? REPayment { get; set; }
    public decimal? NBEReceipt { get; set; }
    public decimal? NBEPayment { get; set; }
    public string? RemarksReceipt { get; set; }
    public string? RemarksPayment { get; set; }
    public bool IsFrozen { get; set; }
}

public class SavePublicAccountReceiptPaymentDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? MajorHeadId { get; set; }
    public decimal? ActualReceipt { get; set; }
    public decimal? ActualPayment { get; set; }
    public decimal? BalanceAtEndReceipt { get; set; }
    public decimal? BalanceAtEndPayment { get; set; }
    public decimal? BEReceipt { get; set; }
    public decimal? BEPayment { get; set; }
    public decimal? AdjustmentReceipt { get; set; }
    public decimal? AdjustmentPayment { get; set; }
    public decimal? REReceipt { get; set; }
    public decimal? REPayment { get; set; }
    public decimal? NBEReceipt { get; set; }
    public decimal? NBEPayment { get; set; }
    public string? RemarksReceipt { get; set; }
    public string? RemarksPayment { get; set; }
}

// --- Appendix III-B: Status of Scheme Appraisal/Approval during the XVI Finance Commission Cycle ---
public class AppendixSchemeAppraisalStatusDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? SchemeId { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string StatusOfFreshAppraisalApproval { get; set; } = string.Empty;
    public DateOnly? SchemeApprovalValidUpto { get; set; }
    public string? Remarks { get; set; }
    public bool IsFrozen { get; set; }
}

public class SaveAppendixSchemeAppraisalStatusDto
{
    public int? Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    // Category / Scheme are now cascading dropdowns (client instruction 2026-09-10) - only the ids
    // are posted; the PreBudget service resolves the display names.
    public int? CategoryId { get; set; }
    public int? SchemeId { get; set; }
    public string? CategoryType { get; set; }
    public string? SchemeName { get; set; }
    public string StatusOfFreshAppraisalApproval { get; set; } = string.Empty;
    public DateOnly? SchemeApprovalValidUpto { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Appendix III-B Category dropdown option (dbo.M_Category, current FY, SerialNo II/IV).</summary>
public class AppendixIIIBCategoryOptionDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

// --- Appendix permission framework (VII-A/VII-B/X/PA-ReceiptPayment) ---
public class AppendixPermissionDto
{
    public string AppendixCode { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanApprove { get; set; }
}
