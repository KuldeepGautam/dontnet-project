# UBIS_Web Page Inventory — 2026-07-21

Full list of every Area/Controller/Action the live menu currently links to, across all 18 roles
(`admin`/Administrator through the most restricted role), gathered directly from the real,
IIS-hosted AIM (`GET /api/userrole/all-roles`) and MenuGenerator (`GET /api/menu/by-role`) — the
exact same two calls `Tools/MenuScaffolder` itself makes. **771 distinct pages across 14 Areas.**

None of these controllers exist in `UBIS_Web/Areas/` yet (confirmed: zero `*Controller.cs` files
under any Area) — every link below currently 404s, including `REMeeting/REDataandReport`, the
specific one reported. See `PAGE_SCAFFOLDING_PLAN.claude` for how this gets closed.

This is a point-in-time snapshot — re-run the same two API calls (or just re-run
`Tools/MenuScaffolder`, which regenerates this exact data internally) if roles/menu data change.

---

### AutonomousGranteeBodies — Autonomous/Grantee Bodies (24 pages)

| Controller | Action | Function |
|---|---|---|
| ECLApprove | AutonomousGranteeDataforApproval | Autonomous/Grantee Data for Approval |
| ECLApprove | DataForReapproval | Data For Reapproval |
| ECLApprove | DataToApprove | Data To Approve |
| ECLApprove | EmployeeDataforApproval | Employee Data for Approval |
| ECLApprove | PayandAllowancesDataforApproval | Pay and Allowances Data for Approval |
| ECLApprove | PensionersDataforApproval | Pensioners Data for Approval |
| ECLApprove | ProvisionForReapprovalAutonomous | Provision For Reapproval Autonomous |
| ECLDataEntry | AddActuals | Add Actuals |
| ECLDataEntry | AddAutonomousGranteeData | Add Autonomous/Grantee Data |
| ECLDataEntry | AddAutonomousGranteeName | Add Autonomous/Grantee Name |
| ECLDataEntry | AddEmployeesData | Add Employees Data |
| ECLDataEntry | AddPensionersData | Add Pensioners Data |
| ECLDataEntry | AddSchemeOutlay | Add Scheme Outlay |
| ECLDataEntry | AutonomousGranteeBodiesStatus | Autonomous/Grantee Bodies Status |
| ECLDataEntry | AutonomousGranteeReports | Autonomous/Grantee Reports |
| ECLDataEntry | PayandAllowancesExpenditure | Pay and Allowances Expenditure |
| ECLMaster | AddAutonomousGranteeName | Add Autonomous/Grantee Name |
| ECLMaster | AddSchemes | Add Schemes |
| ECLReport | AutonomousGranteeBodies | Autonomous/Grantee Bodies |
| ECLReport | AutonomousGranteeBodiesStatus | Autonomous/Grantee Bodies Status |
| ECLReport | DataAnalysis | Data Analysis |
| ECLReport | DemandWiseECLStatus | Demand Wise ECL Status  |
| ECLReport | DemandWiseECLStatusTopLine | Demand Wise ECL Status Top Line |
| ECLReport | PendingDemand | Pending Demand |

### CombinedDashboard — Combined Dashboard (9 pages)

| Controller | Action | Function |
|---|---|---|
| Dashboard | Circular | Circular |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |

### ContingencyAdvance — Contingency Advance (48 pages)

| Controller | Action | Function |
|---|---|---|
| CFIApprove | CFIDataforApproval | CFI Data for Approval |
| CFIApprove | CFIDataforApprovalbySD | CFI Data for Approval by SD |
| DataEntry | AddChargedRecoveries | Add Charged/Recoveries |
| DataEntry | AddPSECategory | Add PSE Category |
| DataEntry | AddSBE | Add SBE |
| DataEntry | CancellationofROsFromPFMS | Cancellation of ROs From PFMS |
| DataEntry | CFIDataforSurrenderAmount | CFI Data for Surrender Amount |
| DataEntry | CFIInitialization | CFI Initialization |
| DataEntry | CFINewMinorHead | CFI New Minor Head |
| DataEntry | ChangePreviousYearBE | Change Previous Year BE |
| DataEntry | ContingencyAdvanceProposal | Contingency Advance Proposal |
| DataEntry | DemandSchemeCeilingAllocation | Demand Scheme Ceiling Allocation |
| DataEntry | DemandSchemeCeilingRelaxation | Demand Scheme Ceiling Relaxation |
| DataEntry | ImportActualsFromDDG | Import Actuals From DDG |
| DataEntry | InvestmentinPSE | Investment in PSE |
| DataEntry | NotesforNTR | Notes for NTR |
| DataEntry | PendingReappropriations | Pending Reappropriations |
| DataEntry | PrepareDataforPushingtoPFMS | Prepare Data  for Pushing to PFMS |
| DataEntry | ReAppropriationNew | ReAppropriation New |
| DataEntry | ReceiptBudgetNTR | Receipt Budget NTR |
| DataEntry | RejectSelfApprovedReappropriation | Reject Self Approved Reappropriation |
| DataEntry | RenumberPSESrNo | Renumber PSE SrNo |
| DataEntry | RenumberSBENotes | Renumber SBE Notes |
| DataEntry | RenumberSchemeSrNo | Renumber Scheme SrNo |
| DataEntry | RenumberSubSchemeSrNo | Renumber SubScheme SrNo |
| DataEntry | RequestforIPChange | Request for IP Change |
| DataEntry | SBEFreeze | SBE Freeze |
| DataEntry | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| DataEntry | SBENotes | SBE Notes |
| DataEntry | SBEPageBreak | SBE Page Break |
| Reports | ApprovedCFISanctions | Approved CFI Sanctions |
| Reports | CFIResumption | CFI Resumption |
| Reports | DDGHeadofAccountsnotmapped | DDG Head of Accounts not mapped |
| Reports | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| Reports | GenerateDDG | Generate DDG |
| Reports | HOAnotInDDG | HOA not In DDG |
| Reports | HOAnotInPFMS | HOA not In PFMS |
| Reports | IssuedCFIProposals | Issued CFI Proposals |
| Reports | MEPQEPReport | MEP/QEP Report |
| Reports | MismatchSummaryofDDGwithSBE | Mismatch Summary of DDG with SBE |
| Reports | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| Reports | ReAppropriationDetails | ReAppropriation Details |
| Reports | ReAppropriationMiscellaneousQueries | ReAppropriation Miscellaneous Queries |
| Reports | ReappropriationStatus | Reappropriation Status |
| Reports | SBESchemesnotmapped | SBE Schemes not mapped |
| Reports | StatusofDDGDemand | Status of DDG Demand |
| Reports | SumMismatchofDDGwithDG | Sum Mismatch of DDG with DG |
| Reports | TokenSupplementaryReport | Token Supplementary Report |

### DDG — DDG (41 pages)

| Controller | Action | Function |
|---|---|---|
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | ViewFileSharing | View File Sharing |
| DDG | DataEntryDDGNew | Data Entry DDG (New) |
| DDG | EditHindiHOA | Edit Hindi HOA |
| DDG | FreezeDDG | Freeze DDG |
| DDG | QEPData | QEP Data |
| DDG | TransferActualforAllDemands | Transfer Actual for All Demands |
| DDG | TransferDDGDatatoSBE | Transfer DDG Data to SBE |
| DDGAllocation | ActualsChangePermission | Actuals Change Permission |
| DDGAllocation | AllowUpdationofConsumedData | Allow Updation of Consumed Data |
| DDGAllocation | FreezeUnfreezeForAllDemand | Freeze/Unfreeze For All Demand |
| DDGAllocation | QEPUnfreeze | QEP Unfreeze |
| DDGAllocation | UserAllocation | User Allocation |
| DDGMasters | AddHOAinACTDRX | Add HOA in ACTDRX |
| Reports | ApprovedCFISanctions | Approved CFI Sanctions |
| Reports | CFIResumption | CFI Resumption |
| Reports | DDGHeadofAccountsnotmapped | DDG Head of Accounts not mapped |
| Reports | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| Reports | GenerateDDG | Generate DDG |
| Reports | HOAnotInDDG | HOA not In DDG |
| Reports | HOAnotInPFMS | HOA not In PFMS |
| Reports | IssuedCFIProposals | Issued CFI Proposals |
| Reports | MEPQEPReport | MEP/QEP Report |
| Reports | MismatchSummaryofDDGwithSBE | Mismatch Summary of DDG with SBE |
| Reports | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| Reports | ReAppropriationDetails | ReAppropriation Details |
| Reports | ReAppropriationMiscellaneousQueries | ReAppropriation Miscellaneous Queries |
| Reports | ReappropriationStatus | Reappropriation Status |
| Reports | SBESchemesnotmapped | SBE Schemes not mapped |
| Reports | StatusofDDGDemand | Status of DDG Demand |
| Reports | SumMismatchofDDGwithDG | Sum Mismatch of DDG with DG |
| Reports | TokenSupplementaryReport | Token Supplementary Report |

### DebtModule — Debt Module (48 pages)

| Controller | Action | Function |
|---|---|---|
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | ViewFileSharing | View File Sharing |
| DataEntry | DatedSecurityAuctionDetails | Dated Security Auction Details |
| DataEntry | DCPDetails | DCP Details |
| DataEntry | DCPExcelFileImport | DCP Excel File Import |
| DataEntry | HistoricDataonLiability | Historic Data on Liability |
| DataEntry | IntermediateLiability | Intermediate Liability |
| DataEntry | OtherLiability | Other Liability |
| DataEntry | TreasuryBills | Treasury Bills |
| DataVisualization | DCPMonthlyChart | DCP Monthly Chart |
| DataVisualization | WeightedAverageGraph |  Weighted  Average Graph |
| Masters | AddCategory | Add Category |
| Masters | AddMajorHead | Add Major Head |
| Masters | AddSchemes | Add Schemes |
| Masters | AddSubCategory | Add SubCategory |
| Masters | AddSubSchemes | Add Sub Schemes |
| Masters | AddUmbrellaSchemes | Add Umbrella Schemes |
| Masters | Category | Category |
| Masters | DatedSecurities | Dated Securities |
| Masters | DCPReceiptandPaymentDetails | DCP Receipt and Payment Details |
| Masters | DCPReceiptPaymentSource | DCP Receipt Payment Source |
| Masters | DebtLiability | Debt   Liability |
| Masters | DemandContactDetails | Demand Contact Details |
| Masters | DemandMajorHeadMapping | Demand MajorHead Mapping |
| Masters | Group | Group |
| Masters | InterestRateOfWMAOD | Interest Rate Of WMA & OD |
| Masters | MajorHead | Major Head |
| Masters | PublicDebt | Public Debt |
| Masters | ReissuedSecurities | Reissued Securities |
| Masters | SBELayout | SBE Layout |
| Masters | Scheme | Scheme |
| Masters | Securities | Securities |
| Masters | SubScheme | SubScheme |
| Reports | DailyCashPosition | Daily Cash Position |
| Reports | DailyInterestOnSurplusCashInvst | Daily Interest On Surplus Cash Invst |
| Reports | DetailedMonthlyLiabilityStatement | Detailed Monthly Liability Statement |
| Reports | Liability | Liability |
| Reports | SummarizedLiabilityStatement | Summarized Liability Statement |
| Reports | WMAODInterest | WMA/OD Interest |

### ECL — ECL (24 pages)

| Controller | Action | Function |
|---|---|---|
| ECLApprove | AutonomousGranteeDataforApproval | Autonomous/Grantee Data for Approval |
| ECLApprove | DataForReapproval | Data For Reapproval |
| ECLApprove | DataToApprove | Data To Approve |
| ECLApprove | EmployeeDataforApproval | Employee Data for Approval |
| ECLApprove | PayandAllowancesDataforApproval | Pay and Allowances Data for Approval |
| ECLApprove | PensionersDataforApproval | Pensioners Data for Approval |
| ECLApprove | ProvisionForReapprovalAutonomous | Provision For Reapproval Autonomous |
| ECLDataEntry | AddActuals | Add Actuals |
| ECLDataEntry | AddAutonomousGranteeData | Add Autonomous/Grantee Data |
| ECLDataEntry | AddAutonomousGranteeName | Add Autonomous/Grantee Name |
| ECLDataEntry | AddEmployeesData | Add Employees Data |
| ECLDataEntry | AddPensionersData | Add Pensioners Data |
| ECLDataEntry | AddSchemeOutlay | Add Scheme Outlay |
| ECLDataEntry | AutonomousGranteeBodiesStatus | Autonomous/Grantee Bodies Status |
| ECLDataEntry | AutonomousGranteeReports | Autonomous/Grantee Reports |
| ECLDataEntry | PayandAllowancesExpenditure | Pay and Allowances Expenditure |
| ECLMaster | AddAutonomousGranteeName | Add Autonomous/Grantee Name |
| ECLMaster | AddSchemes | Add Schemes |
| ECLReport | AutonomousGranteeBodies | Autonomous/Grantee Bodies |
| ECLReport | AutonomousGranteeBodiesStatus | Autonomous/Grantee Bodies Status |
| ECLReport | DataAnalysis | Data Analysis |
| ECLReport | DemandWiseECLStatus | Demand Wise ECL Status  |
| ECLReport | DemandWiseECLStatusTopLine | Demand Wise ECL Status Top Line |
| ECLReport | PendingDemand | Pending Demand |

### ExpBudgetSBE — Exp Budget(SBE) (322 pages)

| Controller | Action | Function |
|---|---|---|
| AdhocQueries | AdhocQueriesforBAAGDetails | Adhoc Queries for BAAG Details |
| AdhocQueries | AdhocQueriesReport | Adhoc Queries Report |
| AdhocQueries | AdhocQueriesStatement18TransferofResources | Adhoc Queries Statement 18/Transfer of Resources |
| AFS | AddAFSForUT | Add AFS For UT |
| AFS | AddCalculatedCashforPublicAccount | Add Calculated Cash for Public Account |
| AFS | AddDataforPublicAccount | Add  Data for Public Account |
| AFS | AddGroup | Add Group |
| AFS | AddScheme | Add Scheme |
| AFS | AddSubScheme | Add SubScheme |
| Allocation | ActualsBECategoryChangePermission | Actuals/BE/Category Change Permission |
| Allocation | ApproveIPAddresses | Approve IP Addresses |
| Allocation | EnableBEEditPermission | Enable BE Edit Permission |
| Allocation | FreezeUnfreezeDemandStatementWise | Freeze/ Unfreeze Demand Statement Wise |
| Allocation | FreezeUnfreezeSBE | Freeze/Unfreeze SBE |
| Allocation | PermissionReport | Permission Report |
| Allocation | RailwayAllocation | Railway Allocation |
| Allocation | REMeetingAllocation | RE Meeting Allocation |
| Allocation | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Allocation | StatementAllocation | Statement Allocation |
| Allocation | UserAllocation | User  Allocation |
| Allocation | VoAAllocation | VoA Allocation |
| Annex | EditAnnexData | Edit Annex Data |
| Annex | EditSanctionData | Edit Sanction Data |
| Annex | StateMaster | State Master |
| Annex | StateValue | State Value |
| Annex | TaxValue | Tax Value |
| AuditLog | AuditLog | Audit Log |
| AuditLog | DeleteAuditErrorLogData | Delete Audit/Error Log Data |
| BAG | AddNotes | Add Notes |
| BAG | BudgetataGlanceValues | Budget at a Glance Values |
| BAG | CompositionOfExpenditure | Composition Of Expenditure |
| BAG | DeficitTrends | Deficit Trends |
| BAG | EconomicParameterfor15to18ofBAAG | Economic Parameter for 15 to 18 of BAAG  |
| BAG | EditUnit | Edit Unit |
| BAG | MajorSchemeMapping | Major Scheme Mapping |
| BAG | MappingforMajorItems | Mapping for Major Items |
| BAG | NetReceiptOfCentre | Net Receipt Of Centre |
| BAG | RupeesChart | Rupees Chart |
| BAG | Sector | Sector |
| BAG | SectorwiseMapping | Sector wise Mapping |
| BAG | SourcesofDeficitFinancing | Sources of Deficit Financing |
| BAG | SubSector | SubSector |
| BAG | TotalTransfersToStates | Total Transfers To States |
| BAG | TrendInTaxReceipts | TrendIn Tax Receipts |
| BAG | TrendOfCapitalExpenditure | Trend Of Capital Expenditure |
| BAGReport | AllBudgetataGlanceReport | All Budget at a Glance Report |
| BAGReport | GraphicalChart | Graphical Chart |
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | SelectReceiptDemand | Select  Receipt Demand |
| Dashboard | ViewFileSharing | View File Sharing |
| DataEntry | AddChargedRecoveries | Add Charged/Recoveries |
| DataEntry | AddPSECategory | Add PSE Category |
| DataEntry | AddSBE | Add SBE |
| DataEntry | CancellationofROsFromPFMS | Cancellation of ROs From PFMS |
| DataEntry | CFIDataforSurrenderAmount | CFI Data for Surrender Amount |
| DataEntry | CFIInitialization | CFI Initialization |
| DataEntry | CFINewMinorHead | CFI New Minor Head |
| DataEntry | ChangePreviousYearBE | Change Previous Year BE |
| DataEntry | ContingencyAdvanceProposal | Contingency Advance Proposal |
| DataEntry | DemandSchemeCeilingAllocation | Demand Scheme Ceiling Allocation |
| DataEntry | DemandSchemeCeilingRelaxation | Demand Scheme Ceiling Relaxation |
| DataEntry | ImportActualsFromDDG | Import Actuals From DDG |
| DataEntry | InvestmentinPSE | Investment in PSE |
| DataEntry | NotesforNTR | Notes for NTR |
| DataEntry | PendingReappropriations | Pending Reappropriations |
| DataEntry | PrepareDataforPushingtoPFMS | Prepare Data  for Pushing to PFMS |
| DataEntry | ReAppropriationNew | ReAppropriation New |
| DataEntry | ReceiptBudgetNTR | Receipt Budget NTR |
| DataEntry | RejectSelfApprovedReappropriation | Reject Self Approved Reappropriation |
| DataEntry | RenumberPSESrNo | Renumber PSE SrNo |
| DataEntry | RenumberSBENotes | Renumber SBE Notes |
| DataEntry | RenumberSchemeSrNo | Renumber Scheme SrNo |
| DataEntry | RenumberSubSchemeSrNo | Renumber SubScheme SrNo |
| DataEntry | RequestforIPChange | Request for IP Change |
| DataEntry | SBEFreeze | SBE Freeze |
| DataEntry | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| DataEntry | SBENotes | SBE Notes |
| DataEntry | SBEPageBreak | SBE Page Break |
| DataExchange | DDGDatabeforetransfertoPFMS | DDG Data before transfer to PFMS |
| DataExchange | DGMHwisebeforetransfertoPFMS | DG MH wise  before transfer to PFMS |
| DataExchange | DGSummarybeforetransfertoPFMS | DG Summary before transfer to PFMS |
| DataExchange | DirectoryMismatchBeforePushingtoPFMS | Directory Mismatch Before Pushing  to  PFMS |
| DataExchange | ExportDDGDatatoPFMS | Export DDG Data to PFMS |
| DataExchange | ExportDGMajorHeadWisetoPFMS | Export DG MajorHead Wise to PFMS |
| DataExchange | ExportDGSummarytoPFMS | Export DG Summary to PFMS |
| DataExchange | ExportReappropriationtoPFMS | Export Reappropriation to PFMS |
| DataExchange | ExportSupplementaryDatatoPFMS | Export Supplementary Data to PFMS |
| DataExchange | FileSharing | File Sharing |
| DataExchange | ImportAccountCodeDirectorytoUBIS | Import Account Code Directory to UBIS |
| DataExchange | ImportActualsFromCGA | Import Actuals From CGA |
| DataExchange | REandNBEmismatchfortransferringdata | RE and NBE mismatch for transferring data |
| DataExchange | SumMismatchofDDGwithDG | Sum Mismatch of DDG with DG |
| DataExchange | UpdateConsumedStatus | Update Consumed Status |
| IntrimInitialization | RevisedSBEDemandNo | Revised SBE Demand No |
| IntrimInitialization | RevisedSTMTDemandNo | Revised STMT Demand No |
| IntrimInitialization | SBE | SBE |
| IntrimInitialization | Statement | Statement |
| MachineReadable | MachineReadableData | Machine Readable Data |
| MasterEntry | AddDemand | Add Demand |
| MasterEntry | AddDepartment | Add Department |
| MasterEntry | AddMinistry | Add Ministry |
| MasterEntry | DateTimeStampforSBEReport | Date Time Stamp for SBE Report |
| MasterEntry | DemandSplit | Demand Split |
| MasterEntry | FAQ | FAQ |
| MasterEntry | FinancialYear | Financial Year |
| MasterEntry | Function | Function |
| MasterEntry | MergeDemandNo | Merge Demand No |
| MasterEntry | Module | Module |
| MasterEntry | RenumberDemandNo | Renumber DemandNo |
| MasterEntry | RenumberDepartment | Renumber Department |
| MasterEntry | RenumberMinistry | Renumber Ministry |
| MasterEntry | Role | Role |
| MasterEntry | RoleFunctionMapping | Role Function Mapping |
| MasterEntry | RoleModuleMapping | Role Module Mapping |
| MasterEntry | SonataTemplate | Sonata Template |
| Masters | AddCategory | Add Category |
| Masters | AddMajorHead | Add Major Head |
| Masters | AddSchemes | Add Schemes |
| Masters | AddSubCategory | Add SubCategory |
| Masters | AddSubSchemes | Add Sub Schemes |
| Masters | AddUmbrellaSchemes | Add Umbrella Schemes |
| Masters | DebtLiability | Debt   Liability |
| Masters | DemandContactDetails | Demand Contact Details |
| Masters | DemandMajorHeadMapping | Demand MajorHead Mapping |
| Masters | PublicDebt | Public Debt |
| Masters | SBELayout | SBE Layout |
| Masters | Securities | Securities |
| MTEFInitialization | MTEF | MTEF |
| NTRDataEntry | AddNotesNTR | Add Notes NTR |
| NTRDataEntry | ReceiptBudgetNTRGroupWise | Receipt Budget NTR Group Wise |
| NTRDataEntry | ReceiptBudgetNTRMHWise | Receipt Budget NTR MH Wise |
| NTRReport | NTR | NTR |
| ReceiptBudget | AddGroup | Add Group |
| ReceiptBudget | AddProgram | Add Program |
| ReceiptBudget | AddReceiptBudget | Add Receipt Budget |
| ReceiptBudget | AddReceiptBudgetNote | Add Receipt Budget Note |
| ReceiptBudget | AddScheme | Add Scheme |
| ReceiptBudget | AddSubScheme | Add SubScheme |
| ReceiptBudget | RenumberProgramSrNo | Renumber Program SrNo |
| ReceiptBudget | RenumberReceiptNotes | Renumber Receipt Notes |
| ReceiptBudget | RenumberSchemeSrNo | Renumber SchemeSrNo |
| ReceiptBudget | RenumberSubProgramSrNo | Renumber SubProgramSrNo |
| ReceiptBudget | RenumberSubSchemeSrNo | Renumber SubScheme SrNo |
| Reports | AFS | AFS |
| Reports | AnnexReport | Annex Report |
| Reports | ArrearsOfNTR | Arrears Of NTR |
| Reports | AssetRegister | Asset Register |
| Reports | CategoryWiseSchemes | Category Wise Schemes |
| Reports | CombinedAPartTopLines | Combined A Part Top Lines |
| Reports | CumalativeSanctionDevolutionReport | Cumalative Sanction Devolution Report |
| Reports | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| Reports | DemandSchemeCeillingAllocation | Demand Scheme Ceilling Allocation |
| Reports | DGSummary | DG Summary |
| Reports | GenerateDG | Generate DG |
| Reports | GenerateSBE | Generate SBE |
| Reports | GenerateSBEfromOtherSource | Generate SBE from Other Source |
| Reports | GenerateSBESSRS | Generate SBE (SSRS) |
| Reports | GenerateSBEUserWise | Generate SBE UserWise |
| Reports | InvestmentInPSE | Investment In PSE |
| Reports | InvestmentInPSEReport | Investment In PSE Report |
| Reports | MajorHeadDemandCategoryWiseSchemeSubScheme | 	MajorHead/Demand/Category Wise Scheme/SubScheme |
| Reports | NTR | NTR |
| Reports | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| Reports | ReceiptNTR | Receipt NTR |
| Reports | ReceiptRecoveryChargedReport | Receipt/Recovery/Charged Report |
| Reports | Receipts | Receipts |
| Reports | ReceiptsSSRS | Receipts(SSRS) |
| Reports | RelaxedSchemesUnderECL | Relaxed Schemes Under ECL  |
| Reports | SanctionReport | Sanction Report |
| Reports | SBEBEDifference | SBE BE Difference  |
| Reports | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Reports | SBEReportInExcel | SBE Report In Excel |
| Reports | SBEReportWithoutData | SBE Report Without Data |
| Reports | VoteonAccount | Vote on Account |
| Reports | VoteonAccountInterim | Vote on Account (Interim) |
| Reports | VoteonAccountRegular | Vote on Account (Regular) |
| ReportsAFS | AFS | AFS |
| ReportsAFS | Receipts | Receipts |
| ReportsAFS | ReceiptsSSRS | Receipts(SSRS) |
| ReportsBAG | AllBudgetataGlanceReport | All Budget at a Glance Report |
| ReportsBAG | GraphicalChart | Graphical Chart |
| ReportsDDG | DDGHeadofAccountsnotmapped | DDG Head of Accounts not mapped |
| ReportsDDG | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| ReportsDDG | GenerateDDG | Generate DDG |
| ReportsDDG | HOAnotInDDG | HOA not In DDG |
| ReportsDDG | HOAnotInPFMS | HOA not In PFMS |
| ReportsDDG | MismatchSummaryofDDGwithSBE | Mismatch Summary of DDG with SBE |
| ReportsDDG | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| ReportsDDG | SBESchemesnotmapped | SBE Schemes not mapped |
| ReportsDDG | StatusofDDGDemand | Status of DDG Demand |
| ReportsDDG | SumMismatchofDDGwithDG | Sum Mismatch of DDG with DG |
| ReportsECL | DataAnalysis | Data Analysis |
| ReportsECL | DemandWiseECLStatus | Demand Wise ECL Status  |
| ReportsECL | DemandWiseECLStatusTopLine | Demand Wise ECL Status Top Line |
| ReportsECL | PendingDemand | Pending Demand |
| ReportsRE | REConsolidated | RE Consolidated |
| ReportsRE | REDataandReport | RE Data and Report |
| ReportsRE | REReportForAllDemand | RE Report For All Demand  |
| ReportsReceipt | ArrearsOfNTR | Arrears Of NTR |
| ReportsReceipt | ArrearsOfNTRAnalyticsReport | Arrears Of NTR Analytics Report |
| ReportsReceipt | AssetRegister | Asset Register |
| ReportsReceipt | AssetRegisterAnalytics | Asset Register Analytics |
| ReportsReceipt | NSSFAnnexVIIISSRS | NSSF Annex VIII(SSRS) |
| ReportsReceipt | NSSFStmtNoThreeReport | NSSF(Stmt No Three) Report |
| ReportsStatement | AdhocQueriesforStatement18 | Adhoc Queries for Statement 18 |
| ReportsStatement | AllStatementReports | All Statement Reports |
| ReportsStatement | CheckBEDifferenceforStatement | Check BE Difference  for Statement |
| ReportsStatement | GenerateZipforAllStatement | Generate Zip for All Statement |
| ReportsStatement | RailwayStatementsReport | Railway Statements Report |
| ReportsStatement | StatementReport | Statement Report |
| ReportsStatement | StatementReportsDirectlyGeneratedFromDDG | Statement Reports Directly Generated From DDG |
| ReportsSupplementary | CheckSheet | Check Sheet |
| ReportsSupplementary | GenerateSupplementaryApprovedbySD | Generate Supplementary (Approved by SD) |
| ReportsSupplementary | GenerateSupplementaryWithLineBreak | Generate Supplementary With Line Break |
| ReportsSupplementary | PreviousSupplementaryDemandWise | Previous Supplementary DemandWise |
| ReportsSupplementary | ReporttoParliamentAnnexure | Report to Parliament Annexure |
| ReportsSupplementary | SupplementaryReportsMiscellaneous | Supplementary Reports (Miscellaneous) |
| ReportsUBIS | CategoryWiseSchemeSubscheme | Category Wise Scheme/Subscheme |
| ReportsUBIS | ChargedReport | Charged Report |
| ReportsUBIS | CombinedAPartTopLines | Combined A Part Top Lines |
| ReportsUBIS | DemandWiseSchemeSubScheme | Demand Wise Scheme/SubScheme |
| ReportsUBIS | DGSummary | DG Summary |
| ReportsUBIS | GenerateDG | Generate DG |
| ReportsUBIS | GenerateSBE | Generate SBE |
| ReportsUBIS | GenerateSBEfromOtherSource | Generate SBE from Other Source |
| ReportsUBIS | GenerateSBESSRS | Generate SBE (SSRS) |
| ReportsUBIS | GenerateZipforAllDGDemand | Generate Zip for All DG Demand |
| ReportsUBIS | GenerateZipforAllSBEDemand | Generate Zip for All SBE Demand |
| ReportsUBIS | InvestmentInPSE | Investment In PSE |
| ReportsUBIS | MajorHeadWiseSchemeSubScheme | MajorHeadWise Scheme/SubScheme |
| ReportsUBIS | ReceiptReport | Receipt Report |
| ReportsUBIS | RecoveryReport | Recovery Report |
| ReportsUBIS | SBEBEDifference | SBE BE Difference  |
| ReportsUBIS | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| ReportsUBIS | SBEReportinExcel | SBE Report in Excel |
| ReportsUBIS | SBEReportWithoutData | SBE Report Without Data |
| ReportsVOA | VoteonAccountInterim | Vote on Account (Interim) |
| ReportsVOA | VoteonAccountRegular | Vote on Account (Regular) |
| UBISInitialization | AssetRegister | Asset Register  |
| UBISInitialization | BAGData | BAG Data |
| UBISInitialization | BudgetarySupportIEBRData | Budgetary SupportIEBR Data |
| UBISInitialization | DDG | DDG |
| UBISInitialization | Master | Master |
| UBISInitialization | MinistryDepartmentDemand | Ministry/Department/Demand |
| UBISInitialization | NTRBudget | NTR Budget |
| UBISInitialization | PAAccount | PA Account |
| UBISInitialization | Receipt | Receipt |
| UBISInitialization | ReceiptDataNotes | Receipt Data/Notes |
| UBISInitialization | ReceiptStatement | Receipt Statement |
| UBISInitialization | RecoveriesChargedData | Recoveries Charged Data |
| UBISInitialization | SBECaption | SBE Caption |
| UBISInitialization | SBECaptiondata | SBE Caption data |
| UBISInitialization | SBEData | SBE Data |
| UBISInitialization | SBENotes | SBE Notes |
| UBISInitialization | Statement | Statement |
| UBISInitialization | UserDetailsInitialization | User Details Initialization |
| UBISInitialization | UserInitialization | User Initialization |
| UpdateContact | NodalOfficer | Nodal Officer |
| UpdateHindiBudgetMaster | AddDemand | Add Demand |
| UpdateHindiBudgetMaster | AddDepartment | Add Department |
| UpdateHindiBudgetMaster | AddMinistry | Add Ministry |
| UpdateHindiBudgetMaster | Category | Category |
| UpdateHindiBudgetMaster | EditHindiHOA | Edit Hindi HOA |
| UpdateHindiBudgetMaster | EditHindiSBEData | Edit Hindi SBE Data |
| UpdateHindiBudgetMaster | GenerateSBEInExcel | Generate SBE In Excel |
| UpdateHindiBudgetMaster | InvestmentPSEName | Investment PSE Name |
| UpdateHindiBudgetMaster | MajorHead | Major Head |
| UpdateHindiBudgetMaster | MajorHeadGroupName | Major Head Group Name |
| UpdateHindiBudgetMaster | ObjectHead | Object Head |
| UpdateHindiBudgetMaster | PSECategory | PSE Category |
| UpdateHindiBudgetMaster | SBENotes | SBE Notes |
| UpdateHindiBudgetMaster | Schemes |  Schemes |
| UpdateHindiBudgetMaster | SubCategory | SubCategory |
| UpdateHindiBudgetMaster | SubSchemes | Sub Schemes |
| UpdateHindiBudgetMaster | UmbrellaScheme | Umbrella Scheme |
| UpdateHindiPAReceipt | Group | Group |
| UpdateHindiPAReceipt | Scheme | Scheme |
| UpdateHindiPAReceipt | SubScheme | Sub Scheme |
| UpdateHindiReceiptBudget | Group | Group |
| UpdateHindiReceiptBudget | Program | Program |
| UpdateHindiReceiptBudget | ReceiptNotes | Receipt Notes |
| UpdateHindiReceiptBudget | Scheme | Scheme |
| UpdateHindiReceiptBudget | SubProgramme | Sub Programme |
| UpdateHindiReceiptBudget | SubScheme | Sub Scheme |
| UpdateHindiStatements | 10A10Band10BBStatementwiseScheme | 10A10B and 10BB Statement wise Scheme |
| UpdateHindiStatements | 10A10Band10BBStatementwiseSubScheme | 10A10B and 10BB Statement wise SubScheme |
| UpdateHindiStatements | Group | Group |
| UpdateHindiStatements | Hindifor4A | Hindi for 4A |
| UpdateHindiStatements | Scheme | Scheme |
| UpdateHindiStatements | SpecialSchemeStmt78and18 | Special Scheme ( Stmt 7, 8 and 18 ) |
| UpdateHindiStatements | Statement17 | Statement 17 |
| UpdateHindiStatements | StatementWiseGroup | Statement Wise Group |
| UpdateHindiStatements | Statementwisescheme | Statement wise scheme |
| UpdateHindiStatements | StatementWiseSubscheme | Statement Wise Subscheme |
| UpdateHindiStatements | SubScheme | Sub Scheme |
| UpdateNSSFMaster | EditNSSFGroup | Edit NSSF Group |
| UpdateNSSFMaster | EditNSSFScheme | Edit NSSF Scheme |
| UpdateNSSFMaster | EditNSSFSubScheme | Edit  NSSF SubScheme |
| User | ActivateDeactivateUsers | Activate Deactivate Users |
| User | AppandUserWiseFreezeFunction | App and User Wise Freeze Function |
| User | ChangeStatementsForUser | Change Statements For User |
| User | CreateUser | Create User |
| User | DeletePreviousYearLogData | Delete Previous Year Log Data |
| User | EditUserDetails | Edit User Details |
| User | ResetPassword | Reset Password |
| User | UserWiseChangeDemand | User Wise Change Demand |
| VOA | CommitedLiablities | Commited Liablities |
| VOA | ControlStatement | Control Statement |
| VOA | DemandAllocation | Demand Allocation |
| VOA | IntroNotes | Intro Notes |
| VOA | SchemetobeExcludedfromVOA | Scheme to be Excluded from VOA |

### ExpProfile — Exp Profile (48 pages)

| Controller | Action | Function |
|---|---|---|
| Allocation | ActualsBECategoryChangePermission | Actuals/BE/Category Change Permission |
| Allocation | ApproveIPAddresses | Approve IP Addresses |
| Allocation | EnableBEEditPermission | Enable BE Edit Permission |
| Allocation | FreezeUnfreezeDemandStatementWise | Freeze/ Unfreeze Demand Statement Wise |
| Allocation | FreezeUnfreezeSBE | Freeze/Unfreeze SBE |
| Allocation | PermissionReport | Permission Report |
| Allocation | RailwayAllocation | Railway Allocation |
| Allocation | REMeetingAllocation | RE Meeting Allocation |
| Allocation | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Allocation | StatementAllocation | Statement Allocation |
| Allocation | UserAllocation | User  Allocation |
| Allocation | VoAAllocation | VoA Allocation |
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |
| Railways | OperationRatio | Operation Ratio |
| Railways | RailwaystatementEntry | Railway statement Entry |
| Railways | RailwayStatementsReport | Railway Statements Report |
| Statement | AddStatementNotes | Add Statement Notes |
| Statement | ChangeActualsandPreviousBEforStmt12and13 | Change Actuals and Previous BE for Stmt 12 and 13 |
| Statement | EditDepictedSchemeNameHindifor4A | Edit Depicted Scheme Name/Hindi for 4A |
| Statement | EditSchemeName | Edit Scheme Name |
| Statement | EditSubSchemeName | Edit SubScheme Name |
| Statement | MappingforStatement12and13 | Mapping for Statement 12 and 13 |
| Statement | MappingforStatement4A | Mapping for Statement 4A |
| Statement | MappingofStatement78and18 | Mapping of Statement 7, 8 and 18 |
| Statement | ReceiptStatementEntry | Receipt Statement Entry |
| Statement | RenumberUmbrellaSrNo | Renumber Umbrella SrNo |
| Statement | SchemesMasterForStmt78and18 | Schemes Master For Stmt 7,8 and 18 |
| Statement | StatementEntry | Statement Entry |
| Statement | StatementLayout | Statement Layout |
| StatementReports | AdhocQueriesforStatement18 | Adhoc Queries for Statement 18 |
| StatementReports | AllStatementReports | All Statement Reports |
| StatementReports | CheckBEDifferenceforStatement | Check BE Difference  for Statement |
| StatementReports | Statement2Aand2BinExcel | Statement 2A and 2B in Excel |
| StatementReports | StatementInExcel | Statement In Excel |
| StatementReports | StatementReport | Statement Report |
| StatementReports | StatementReportsDirectlyGeneratedFromDDG | Statement Reports Directly Generated From DDG |

### MTEF — MTEF (43 pages)

| Controller | Action | Function |
|---|---|---|
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |
| Finalization | MTEF | MTEF |
| Finalization | ObjectHeadDataFinalization | Object Head Data Finalization |
| Master | AddMTEFCategory | Add MTEF  Category |
| Master | AddObjectHeadName | Add ObjectHead Name |
| Master | MappingMTEFCategory | Mapping MTEF Category |
| Master | MappingObjectHead | Mapping Object Head |
| Master | MTEFSchemeMapping | MTEF Scheme Mapping |
| Master | MTEFSubSchemeMapping | MTEFSub Scheme Mapping |
| Master | MTEFUmbrellaSchemes | MTEF Umbrella Schemes |
| MTEFAllocation | MTEFDemandAllocation | MTEF Demand Allocation |
| MTEFReports | AnnexureICategoryWise | Annexure I (Category Wise) |
| MTEFReports | AnnexureIDetailed | Annexure I Detailed |
| MTEFReports | AnnexureIIListOfMajorSchemes | Annexure II (List Of Major Schemes) |
| MTEFReports | AnnexureIIListOfMajorSchemesDetails | Annexure II (List Of Major Schemes Details)	 |
| MTEFReports | AnnexureIIMTEFDemandWise | Annexure II (MTEF Demand Wise) |
| MTEFReports | AnnexureIIMTEFDemandWiseForDepartment | Annexure II (MTEF Demand Wise) For Department |
| MTEFReports | DemandWiseProjectionLogReport | Demand Wise Projection Log Report |
| MTEFReports | FinalProjection | Final Projection |
| MTEFReports | MTEFProjection | MTEF Projection |
| MTEFReports | MTEFProjectionLog | MTEF Projection Log |
| MTEFReports | MTEFProjectionsSubmittedbyUsers | MTEF Projections Submitted by Users |
| MTEFReports | MTEFStatus | MTEF Status |
| MTEFReports | MTFPProjection | MTFP Projection |
| MTEFReports | SchemeWiseProjection | SchemeWise Projection |
| MTEFReports | SchemeWiseProjectionLog | Scheme Wise Projection Log |
| MTEFReports | SchemeWiseRevenueCapital | SchemeWise Revenue/Capital |
| Projections | AddMTEFData | Add MTEF Data |
| Projections | AddMTEFObjectheadData | Add MTEF Object head Data |
| Projections | AddMTFPData | Add MTFP Data |
| Projections | AddSchemeWiseProjection | Add SchemeWise Projection |

### NSModule — NSModule (7 pages)

| Controller | Action | Function |
|---|---|---|
| NSEntry | Annex8SourcesapplicationofNSSF | Annex8-Sources & application of NSSF |
| NSEntry | NationalSmallSavingScheme | National Small Saving Scheme  |
| NSMasters | AddGroup | Add Group |
| NSMasters | AddScheme | Add Scheme |
| NSMasters | AddSubScheme | Add SubScheme |
| NSReports | NSSFAnnexVIIISSRS | NSSF Annex VIII(SSRS) |
| NSReports | NSSFStmtNoThreeReport | NSSF(Stmt No Three) Report |

### Reappropriation — Reappropriation (46 pages)

| Controller | Action | Function |
|---|---|---|
| DataEntry | AddChargedRecoveries | Add Charged/Recoveries |
| DataEntry | AddPSECategory | Add PSE Category |
| DataEntry | AddSBE | Add SBE |
| DataEntry | CancellationofROsFromPFMS | Cancellation of ROs From PFMS |
| DataEntry | CFIDataforSurrenderAmount | CFI Data for Surrender Amount |
| DataEntry | CFIInitialization | CFI Initialization |
| DataEntry | CFINewMinorHead | CFI New Minor Head |
| DataEntry | ChangePreviousYearBE | Change Previous Year BE |
| DataEntry | ContingencyAdvanceProposal | Contingency Advance Proposal |
| DataEntry | DemandSchemeCeilingAllocation | Demand Scheme Ceiling Allocation |
| DataEntry | DemandSchemeCeilingRelaxation | Demand Scheme Ceiling Relaxation |
| DataEntry | ImportActualsFromDDG | Import Actuals From DDG |
| DataEntry | InvestmentinPSE | Investment in PSE |
| DataEntry | NotesforNTR | Notes for NTR |
| DataEntry | PendingReappropriations | Pending Reappropriations |
| DataEntry | PrepareDataforPushingtoPFMS | Prepare Data  for Pushing to PFMS |
| DataEntry | ReAppropriationNew | ReAppropriation New |
| DataEntry | ReceiptBudgetNTR | Receipt Budget NTR |
| DataEntry | RejectSelfApprovedReappropriation | Reject Self Approved Reappropriation |
| DataEntry | RenumberPSESrNo | Renumber PSE SrNo |
| DataEntry | RenumberSBENotes | Renumber SBE Notes |
| DataEntry | RenumberSchemeSrNo | Renumber Scheme SrNo |
| DataEntry | RenumberSubSchemeSrNo | Renumber SubScheme SrNo |
| DataEntry | RequestforIPChange | Request for IP Change |
| DataEntry | SBEFreeze | SBE Freeze |
| DataEntry | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| DataEntry | SBENotes | SBE Notes |
| DataEntry | SBEPageBreak | SBE Page Break |
| Reports | ApprovedCFISanctions | Approved CFI Sanctions |
| Reports | CFIResumption | CFI Resumption |
| Reports | DDGHeadofAccountsnotmapped | DDG Head of Accounts not mapped |
| Reports | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| Reports | GenerateDDG | Generate DDG |
| Reports | HOAnotInDDG | HOA not In DDG |
| Reports | HOAnotInPFMS | HOA not In PFMS |
| Reports | IssuedCFIProposals | Issued CFI Proposals |
| Reports | MEPQEPReport | MEP/QEP Report |
| Reports | MismatchSummaryofDDGwithSBE | Mismatch Summary of DDG with SBE |
| Reports | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| Reports | ReAppropriationDetails | ReAppropriation Details |
| Reports | ReAppropriationMiscellaneousQueries | ReAppropriation Miscellaneous Queries |
| Reports | ReappropriationStatus | Reappropriation Status |
| Reports | SBESchemesnotmapped | SBE Schemes not mapped |
| Reports | StatusofDDGDemand | Status of DDG Demand |
| Reports | SumMismatchofDDGwithDG | Sum Mismatch of DDG with DG |
| Reports | TokenSupplementaryReport | Token Supplementary Report |

### ReceiptBudget — Receipt Budget (40 pages)

| Controller | Action | Function |
|---|---|---|
| Allocation | AssestRegisterAllocation | Assest Register Allocation |
| Dashboard | AssetRegister | Asset Register |
| Dashboard | Circular | Circular |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |
| NSEntry | Annex8SourcesapplicationofNSSF | Annex8-Sources & application of NSSF |
| NSEntry | NationalSmallSavingScheme | National Small Saving Scheme  |
| NSMasters | AddGroup | Add Group |
| NSMasters | AddScheme | Add Scheme |
| NSMasters | AddSubScheme | Add SubScheme |
| NSReports | NSSFAnnexVIIISSRS | NSSF Annex VIII(SSRS) |
| NSReports | NSSFStmtNoThreeReport | NSSF(Stmt No Three) Report |
| RBudget | ArrearsofNTR | Arrears of NTR |
| RBudget | AssetRegister | Asset Register |
| Reports | ArrearsOfNTR | Arrears Of NTR |
| Reports | ArrearsOfNTRAnalyticsReport | Arrears Of NTR Analytics Report |
| Reports | AssetRegister | Asset Register |
| Reports | AssetRegisterAnalytics | Asset Register Analytics |
| Reports | DemandCeilingValidationsChecklist | Demand Ceiling Validations Checklist |
| Reports | GenerateDG | Generate DG |
| Reports | GenerateSBEUserWise | Generate SBE UserWise |
| Reports | InvestmentInPSEReport | Investment In PSE Report |
| Reports | NTR | NTR |
| Reports | REandNBEsummismatchfortransferringdata | RE and NBE sum mismatch for transferring data |
| Reports | ReceiptRecoveryChargedReport | Receipt/Recovery/Charged Report |
| Reports | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Reports | SBEReportWithoutData | SBE Report Without Data |
| Reports | VoteonAccount | Vote on Account |
| RMaster | Group | Group |
| RMaster | Scheme | Scheme |
| RMaster | SubScheme | Sub Scheme |
| RStatement | AssetNotes | Asset Notes |
| RStatement | ReceiptLayout | Receipt Layout |
| RStatement | ReceiptNTRLayout | Receipt NTR Layout |

### REMeeting — RE Meeting (30 pages)

| Controller | Action | Function |
|---|---|---|
| Allocation | ActualsBECategoryChangePermission | Actuals/BE/Category Change Permission |
| Allocation | ApproveIPAddresses | Approve IP Addresses |
| Allocation | EnableBEEditPermission | Enable BE Edit Permission |
| Allocation | FreezeUnfreezeDemandStatementWise | Freeze/ Unfreeze Demand Statement Wise |
| Allocation | FreezeUnfreezeSBE | Freeze/Unfreeze SBE |
| Allocation | PermissionReport | Permission Report |
| Allocation | RailwayAllocation | Railway Allocation |
| Allocation | REMeetingAllocation | RE Meeting Allocation |
| Allocation | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Allocation | StatementAllocation | Statement Allocation |
| Allocation | UserAllocation | User  Allocation |
| Allocation | VoAAllocation | VoA Allocation |
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |
| REMeeting | AutonomousMasterforAppendixVICandVIE | Autonomous Master for Appendix VI C and VI E |
| REMeeting | REConsolidated | RE Consolidated |
| REMeeting | REDataandReport | RE Data and Report |
| REMeeting | REDataRemarks | RE Data Remarks |
| REMeeting | REReportForAllDemand | RE Report For All Demand  |

### SupplementaryBudget — Supplementary Budget (41 pages)

| Controller | Action | Function |
|---|---|---|
| Dashboard | Circular | Circular |
| Dashboard | CircularReport | Circular Report |
| Dashboard | CombinedDashboard | Combined Dashboard |
| Dashboard | CreateCircularLetter | Create Circular/Letter |
| Dashboard | DemIdDemNoMismatch | DemId DemNo Mismatch |
| Dashboard | FileSharing | File Sharing |
| Dashboard | ListofCirculars | List of Circulars |
| Dashboard | SBELineEntryDeletePermission | SBE Line Entry Delete Permission |
| Dashboard | SBEStatusReport | SBE Status Report |
| Dashboard | SectionWiseDemandReport | Section Wise Demand Report |
| Dashboard | SelectDemand | Select Demand |
| Dashboard | SelectDemandSectionWise | Select  Demand Section Wise |
| Dashboard | ViewFileSharing | View File Sharing |
| SuppAllocation | AddSupplementaryNumber | Add Supplementary Number |
| SuppAllocation | DeactiveSupplementaryNumber | Deactive Supplementary Number |
| SuppAllocation | FreezeUnFreezeAnnexure | Freeze UnFreeze Annexure |
| SuppAllocation | MajorHeadMapping | Major Head Mapping |
| SuppAllocation | ReopenafterApproval | Reopen after Approval |
| SuppAllocation | SuppDemandAllocation | Supp Demand Allocation |
| SuppAllocation | TokenReapprRelaxation | Token Reappr Relaxation |
| Suppbudget | AddDemandWisePageNo | Add DemandWise Page No |
| Suppbudget | AddSupplementaryData | Add Supplementary Data |
| Suppbudget | ReceiptRecoveryforTechnicalSupp | Receipt / Recovery for Technical Supp |
| Suppbudget | ReporttoParliamentAnnexure | Report to Parliament Annexure |
| Suppbudget | ReporttoParliamentTimePeriod | Report to Parliament Time Period  |
| Suppbudget | SupplementaryIntroduction | Supplementary Introduction |
| Suppbudget | ViewSupplementary | View Supplementary |
| SupplementaryHindiNotes | UpdateHindiforReporttoParliament | Update Hindi for Report to Parliament |
| SupplementaryHindiNotes | UpdateHindiIntro | Update Hindi Intro |
| SupplementaryHindiNotes | UpdateHindiNotes | Update Hindi Notes |
| SuppReports | CheckSheet | Check Sheet |
| SuppReports | GenerateSupplementaryApprovedbySD | Generate Supplementary (Approved by SD) |
| SuppReports | GenerateSupplementaryReport | Generate Supplementary Report |
| SuppReports | GenerateSupplementaryWithLineBreak | Generate Supplementary With Line Break |
| SuppReports | PreviousSupplementaryDemandWise | Previous Supplementary DemandWise |
| SuppReports | ProposalStatus | Proposal Status |
| SuppReports | RecoupAmountMismatch | Recoup Amount Mismatch |
| SuppReports | ReporttoParliamentAnnexure | Report to Parliament Annexure |
| SuppReports | StatusofReporttoParliamentData | Status of Report to Parliament Data |
| SuppReports | SupplementaryDetailsHOAWise | Supplementary Details HOA Wise |
| SuppReports | SupplementaryReportsMiscellaneous | Supplementary Reports (Miscellaneous) |


