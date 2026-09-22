namespace UBIS.Web.Services.Clients;

/// <summary>Typed HttpClient for the PreBudget microservice (Domain\PreBudget). Added 2026-07-23.</summary>
public interface IPreBudgetClient
{
    Task<ApiCallResult<List<PreBudgetAppendixDto>>> GetAppendicesAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<List<PreBudgetAppendixWithStatusDto>>> GetAppendicesWithStatusAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<object?>> SetNilSubmissionAsync(string bearerToken, SetNilSubmissionDto request, CancellationToken ct = default);

    Task<ApiCallResult<object?>> FreezeSubmissionAsync(string bearerToken, int appendixId, int demandId, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<List<int>>> GetAllocatedDemandIdsAsync(string bearerToken, IEnumerable<int> demandIds, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<AllocationResultDto>> AllocateAsync(string bearerToken, AllocateAppendixDto request, CancellationToken ct = default);

    Task<ApiCallResult<List<AllocationListItemDto>>> GetAllocationsAsync(string bearerToken, int? appendixId, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<object?>> UpdateAllocationAsync(string bearerToken, UpdateAllocationDto request, CancellationToken ct = default);

    // --- Appendix I ---
    Task<ApiCallResult<List<AppendixBudgetExpenditureTrendDto>>> GetAppendixIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixBudgetExpenditureTrendDto?>> GetAppendixIByYearAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> CreateAppendixIAsync(string bearerToken, SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> UpdateAppendixIAsync(string bearerToken, int id, SaveAppendixBudgetExpenditureTrendDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixBudgetExpenditureTrendDto>> FreezeAppendixIAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix II ---
    Task<ApiCallResult<List<AppendixQuarterlyExpenditurePlanDto>>> GetAppendixIIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> CreateAppendixIIAsync(string bearerToken, SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> UpdateAppendixIIAsync(string bearerToken, int id, SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixQuarterlyExpenditurePlanDto>> FreezeAppendixIIAsync(string bearerToken, int id, CancellationToken ct = default);

    /// <summary>Previous-year "As per approved QEP" Q1/Q2 via M_Demand.PrevDemandId + dbo.T_QEPData (client reference SQL, 2026-08-07).</summary>
    Task<ApiCallResult<AppendixIIPreviousYearApprovedQepDto>> GetAppendixIIPreviousYearApprovedQepAsync(string bearerToken, int demandId, CancellationToken ct = default);

    // --- Appendix III-A ---
    Task<ApiCallResult<List<AppendixTsaAssignmentDto>>> GetAppendixIIIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTsaAssignmentDto>> CreateAppendixIIIAAsync(string bearerToken, SaveAppendixTsaAssignmentDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTsaAssignmentDto>> UpdateAppendixIIIAAsync(string bearerToken, int id, SaveAppendixTsaAssignmentDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIIIAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTsaAssignmentDto>> FreezeAppendixIIIAAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix I-A ---
    Task<ApiCallResult<List<AppendixProjectedDemandDto>>> GetAppendixIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixProjectedDemandDto>> CreateAppendixIAAsync(string bearerToken, SaveAppendixProjectedDemandDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixProjectedDemandDto>> UpdateAppendixIAAsync(string bearerToken, int id, SaveAppendixProjectedDemandDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixProjectedDemandDto>> FreezeAppendixIAAsync(string bearerToken, int id, CancellationToken ct = default);

    /// <summary>Previous-year Revenue/Capital BE via M_Demand.PrevDemandId + dbo.SBEData (client reference SQL, 2026-08-07).</summary>
    Task<ApiCallResult<AppendixIAPreviousYearBeDto>> GetAppendixIAPreviousYearBeAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);

    // --- Appendix III ---
    Task<ApiCallResult<List<AppendixCnaSnaBalanceDto>>> GetAppendixIIIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCnaSnaBalanceDto>> CreateAppendixIIIAsync(string bearerToken, SaveAppendixCnaSnaBalanceDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCnaSnaBalanceDto>> UpdateAppendixIIIAsync(string bearerToken, int id, SaveAppendixCnaSnaBalanceDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIIIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCnaSnaBalanceDto>> FreezeAppendixIIIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIICategoryOptionDto>>> GetAppendixIIICategoriesAsync(string bearerToken, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIIISchemesAsync(string bearerToken, int demandId, string categoryType, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixIIISubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default);

    // --- Appendix IV ---
    Task<ApiCallResult<List<AppendixEstimatesOfSchemesDto>>> GetAppendixIVAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> CreateAppendixIVAsync(string bearerToken, SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> UpdateAppendixIVAsync(string bearerToken, int id, SaveAppendixEstimatesOfSchemesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIVAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstimatesOfSchemesDto>> FreezeAppendixIVAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIVSchemesAsync(string bearerToken, int demandId, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixIVSubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default);

    // --- Appendix IV-A ---
    Task<ApiCallResult<List<AppendixScspExpenditureDto>>> GetAppendixIVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixScspExpenditureDto>> CreateAppendixIVAAsync(string bearerToken, SaveAppendixScspExpenditureDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixScspExpenditureDto>> UpdateAppendixIVAAsync(string bearerToken, int id, SaveAppendixScspExpenditureDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIVAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixScspExpenditureDto>> FreezeAppendixIVAAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix IV-B ---
    Task<ApiCallResult<List<AppendixTaspExpenditureDto>>> GetAppendixIVBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTaspExpenditureDto>> CreateAppendixIVBAsync(string bearerToken, SaveAppendixTaspExpenditureDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTaspExpenditureDto>> UpdateAppendixIVBAsync(string bearerToken, int id, SaveAppendixTaspExpenditureDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIVBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixTaspExpenditureDto>> FreezeAppendixIVBAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix V ---
    Task<ApiCallResult<List<AppendixEstablishmentExpenditureDto>>> GetAppendixVAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentExpenditureDto>> CreateAppendixVAsync(string bearerToken, SaveAppendixEstablishmentExpenditureDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentExpenditureDto>> FreezeAppendixVAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix V-A ---
    Task<ApiCallResult<List<AppendixGrantInAidDto>>> GetAppendixVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixGrantInAidDto>> CreateAppendixVAAsync(string bearerToken, SaveAppendixGrantInAidDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixGrantInAidDto>> UpdateAppendixVAAsync(string bearerToken, int id, SaveAppendixGrantInAidDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixGrantInAidDto>> FreezeAppendixVAAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix V-B ---
    Task<ApiCallResult<List<AppendixEstablishmentByObjectHeadDto>>> GetAppendixVBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> CreateAppendixVBAsync(string bearerToken, SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> UpdateAppendixVBAsync(string bearerToken, int id, SaveAppendixEstablishmentByObjectHeadDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentByObjectHeadDto>> FreezeAppendixVBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVBObjectHeadOptionDto>>> GetAppendixVBObjectHeadsAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);

    // --- Appendix V-C ---
    Task<ApiCallResult<List<AppendixEstablishmentOtherThanABDto>>> GetAppendixVCAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> CreateAppendixVCAsync(string bearerToken, SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> UpdateAppendixVCAsync(string bearerToken, int id, SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVCAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixEstablishmentOtherThanABDto>> FreezeAppendixVCAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VI ---
    Task<ApiCallResult<List<AppendixNonTaxRevenueDto>>> GetAppendixVIAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixNonTaxRevenueDto>> CreateAppendixVIAsync(string bearerToken, SaveAppendixNonTaxRevenueDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixNonTaxRevenueDto>> UpdateAppendixVIAsync(string bearerToken, int id, SaveAppendixNonTaxRevenueDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixNonTaxRevenueDto>> FreezeAppendixVIAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIReceiptTypeOptionDto>>> GetAppendixVIReceiptTypesAsync(string bearerToken, CancellationToken ct = default);

    // --- Appendix VI-A ---
    Task<ApiCallResult<List<AppendixUserChargesDto>>> GetAppendixVIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesDto>> CreateAppendixVIAAsync(string bearerToken, SaveAppendixUserChargesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesDto>> UpdateAppendixVIAAsync(string bearerToken, int id, SaveAppendixUserChargesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesDto>> FreezeAppendixVIAAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VI-B ---
    Task<ApiCallResult<List<AppendixPendingLiabilitiesDto>>> GetAppendixVIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixPendingLiabilitiesDto>> CreateAppendixVIBAsync(string bearerToken, SaveAppendixPendingLiabilitiesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixPendingLiabilitiesDto>> UpdateAppendixVIBAsync(string bearerToken, int id, SaveAppendixPendingLiabilitiesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixPendingLiabilitiesDto>> FreezeAppendixVIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIBCategoryOptionDto>>> GetAppendixVIBCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default);

    Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixIIIBeByStructureAsync(string bearerToken, int demandId, int schemeId, CancellationToken ct = default);

    Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixIVBeByStructureAsync(string bearerToken, int demandId, int schemeId, int? subSchemeId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixVIBSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISubSchemeOptionDto>>> GetAppendixVIBSubSchemesAsync(string bearerToken, int schemeId, CancellationToken ct = default);
    Task<ApiCallResult<AppendixBeAutoLoadDto>> GetAppendixVIBBeAsync(string bearerToken, int demandId, int categoryId, int schemeId, CancellationToken ct = default);

    /// <summary>Previous-year BE via M_Demand.PrevDemandId + dbo.DDG, matched by Object Head code (client reference SQL, 2026-08-07).</summary>
    Task<ApiCallResult<AppendixVBBeActualsDto>> GetAppendixVBPreviousYearBeAsync(string bearerToken, int demandId, int objectHeadId, CancellationToken ct = default);

    // --- Appendix VI-C ---
    Task<ApiCallResult<List<AppendixCorpusFundDto>>> GetAppendixVICAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundDto>> CreateAppendixVICAsync(string bearerToken, SaveAppendixCorpusFundDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundDto>> UpdateAppendixVICAsync(string bearerToken, int id, SaveAppendixCorpusFundDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVICAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundDto>> FreezeAppendixVICAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VI-D ---
    Task<ApiCallResult<List<AppendixInternalResourcesDto>>> GetAppendixVIDAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixInternalResourcesDto>> CreateAppendixVIDAsync(string bearerToken, SaveAppendixInternalResourcesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixInternalResourcesDto>> UpdateAppendixVIDAsync(string bearerToken, int id, SaveAppendixInternalResourcesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIDAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixInternalResourcesDto>> FreezeAppendixVIDAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VI-E ---
    Task<ApiCallResult<List<AppendixCorpusFundAbGiaDto>>> GetAppendixVIEAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> CreateAppendixVIEAsync(string bearerToken, SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> UpdateAppendixVIEAsync(string bearerToken, int id, SaveAppendixCorpusFundAbGiaDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIEAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCorpusFundAbGiaDto>> FreezeAppendixVIEAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VI-F ---
    Task<ApiCallResult<List<AppendixMinorHeadUserChargesDto>>> GetAppendixVIFAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> CreateAppendixVIFAsync(string bearerToken, SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> UpdateAppendixVIFAsync(string bearerToken, int id, SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIFAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixMinorHeadUserChargesDto>> FreezeAppendixVIFAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<MinorHeadSuggestionDto>>> SearchMinorHeadsAsync(string bearerToken, int demandId, string financialYear, string query, CancellationToken ct = default);
    Task<ApiCallResult<MinorHeadNameDto>> GetMinorHeadNameAsync(string bearerToken, string minorHeadCode, string financialYear, CancellationToken ct = default);

    // --- Appendix III-B ---
    Task<ApiCallResult<List<AppendixSchemeAppraisalStatusDto>>> GetAppendixIIIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> CreateAppendixIIIBAsync(string bearerToken, SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> UpdateAppendixIIIBAsync(string bearerToken, int id, SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixIIIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixSchemeAppraisalStatusDto>> FreezeAppendixIIIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIIBCategoryOptionDto>>> GetAppendixIIIBCategoriesAsync(string bearerToken, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixIIISchemeOptionDto>>> GetAppendixIIIBSchemesAsync(string bearerToken, int demandId, int categoryId, CancellationToken ct = default);

    // --- Appendix VI-G ---
    Task<ApiCallResult<List<AppendixUserChargesAutonomousBodyDto>>> GetAppendixVIGAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> CreateAppendixVIGAsync(string bearerToken, SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> UpdateAppendixVIGAsync(string bearerToken, int id, SaveAppendixUserChargesAutonomousBodyDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIGAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixUserChargesAutonomousBodyDto>> FreezeAppendixVIGAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Appendix VII-A ---
    Task<ApiCallResult<List<AppendixRecoveriesDto>>> GetAppendixVIIAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixRecoveriesDto>> CreateAppendixVIIAAsync(string bearerToken, SaveAppendixRecoveriesDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixRecoveriesDto>> UpdateAppendixVIIAAsync(string bearerToken, int id, SaveAppendixRecoveriesDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIIAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixRecoveriesDto>> FreezeAppendixVIIAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIIAMajorHeadOptionDto>>> GetAppendixVIIAMajorHeadsAsync(string bearerToken, int demandId, CancellationToken ct = default);

    // --- Appendix VII-B ---
    Task<ApiCallResult<List<AppendixCommercialUndertakingReceiptsDto>>> GetAppendixVIIBAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> CreateAppendixVIIBAsync(string bearerToken, SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> UpdateAppendixVIIBAsync(string bearerToken, int id, SaveAppendixCommercialUndertakingReceiptsDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixVIIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixCommercialUndertakingReceiptsDto>> FreezeAppendixVIIBAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIIBSchemeOptionDto>>> GetAppendixVIIBSchemesAsync(string bearerToken, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIIBTransactionTypeOptionDto>>> GetAppendixVIIBTransactionTypesAsync(string bearerToken, CancellationToken ct = default);
    Task<ApiCallResult<List<AppendixVIIBMajorHeadOptionDto>>> GetAppendixVIIBMajorHeadsAsync(string bearerToken, int demandId, int schemeId, CancellationToken ct = default);
    Task<ApiCallResult<AppendixVIIBBeActualsDto>> GetAppendixVIIBBeActualsAsync(string bearerToken, int demandId, int schemeId, int majorHeadId, string transactionType, CancellationToken ct = default);

    // --- Appendix X ---
    Task<ApiCallResult<List<AppendixLoansToGovtServantsDto>>> GetAppendixXAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AppendixLoansToGovtServantsDto>> CreateAppendixXAsync(string bearerToken, SaveAppendixLoansToGovtServantsDto request, CancellationToken ct = default);
    Task<ApiCallResult<AppendixLoansToGovtServantsDto>> UpdateAppendixXAsync(string bearerToken, int id, SaveAppendixLoansToGovtServantsDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixXAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<AppendixLoansToGovtServantsDto>> FreezeAppendixXAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- Public Account Template ---
    Task<ApiCallResult<List<PublicAccountReceiptPaymentDto>>> GetAppendixPAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<PublicAccountReceiptPaymentDto>> CreateAppendixPAAsync(string bearerToken, SavePublicAccountReceiptPaymentDto request, CancellationToken ct = default);
    Task<ApiCallResult<PublicAccountReceiptPaymentDto>> UpdateAppendixPAAsync(string bearerToken, int id, SavePublicAccountReceiptPaymentDto request, CancellationToken ct = default);
    Task<ApiCallResult<object?>> DeleteAppendixPAAsync(string bearerToken, int id, CancellationToken ct = default);
    Task<ApiCallResult<PublicAccountReceiptPaymentDto>> FreezeAppendixPAAsync(string bearerToken, int id, CancellationToken ct = default);

    // --- FR-003: Remarks ---
    Task<ApiCallResult<List<PreBudgetRemarkDto>>> GetRemarksAsync(string bearerToken, int demandId, int appendixId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<PreBudgetRemarkDto>> CreateRemarkAsync(string bearerToken, CreatePreBudgetRemarkDto request, CancellationToken ct = default);

    // --- Appendix permission framework (VII-A/VII-B/X/PA-ReceiptPayment) ---
    Task<ApiCallResult<AppendixPermissionDto>> GetAppendixPermissionsAsync(string bearerToken, string appendixCode, CancellationToken ct = default);

    // --- FR-004: Autonomous Master ---
    Task<ApiCallResult<List<AutonomousBodyDto>>> GetAutonomousBodiesAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<List<AutonomousBodyDto>>> GetAutonomousBodiesForAppendixVAAsync(string bearerToken, int demandId, string financialYear, CancellationToken ct = default);
    Task<ApiCallResult<AutonomousBodyDto>> CreateAutonomousBodyAsync(string bearerToken, CreateAutonomousBodyDto request, CancellationToken ct = default);
    Task<ApiCallResult<AutonomousBodyRequestDto>> RequestNewAutonomousBodyAsync(string bearerToken, CreateAutonomousBodyRequestDto request, CancellationToken ct = default);
    Task<ApiCallResult<List<AutonomousBodyRequestDto>>> GetPendingAutonomousBodyRequestsAsync(string bearerToken, CancellationToken ct = default);
    Task<ApiCallResult<AutonomousBodyRequestDto>> ReviewAutonomousBodyRequestAsync(string bearerToken, int requestId, ReviewAutonomousBodyRequestDto review, CancellationToken ct = default);
}
