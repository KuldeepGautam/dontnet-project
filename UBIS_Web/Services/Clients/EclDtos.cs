namespace UBIS.Web.Services.Clients;

/// <summary>Local DTO copies mirroring Domain/ECL/Application/DTOs/EclDtos.cs (no shared-contracts
/// project in this solution — every service duplicates its own DTOs, UBIS_Web follows the same
/// convention as AimDtos.cs/PreBudgetDtos.cs). Added for the ECL scheme-outlay / DOE-approval
/// front-end (Part 2, 2026-08-18).</summary>
public class EclSchemeOutlayDto
{
    public int RowId { get; set; }
    public string? FinancialYear { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? DemandId { get; set; }
    public int? SchemeId { get; set; }

    /// <summary>Resolved server-side (added 2026-08-19) — always trust this over a client-side
    /// category-scoped lookup, since a row's category can differ from whatever category is
    /// currently selected in the entry form.</summary>
    public string? SchemeName { get; set; }

    /// <summary>Resolved server-side (added 2026-08-19) for grid display ("SchemeSrNo - SchemeName").</summary>
    public int? SchemeSrNo { get; set; }

    /// <summary>Resolved server-side (added 2026-08-19) for grid display ("DemandNo - DemandName") —
    /// DemandNo is the stable cross-year identifier, distinct from DemandId above.</summary>
    public int? DemandNo { get; set; }

    /// <summary>Resolved server-side (added 2026-08-19), paired with DemandNo above.</summary>
    public string? DemandName { get; set; }

    public string? ApproveAuth { get; set; }
    public int? ApprovalAuthorityId { get; set; }
    public decimal? TotalOutlay { get; set; }
    public decimal? CentralSharePercentage { get; set; }
    public decimal? CentralShare { get; set; }
    public decimal?[] Outlay { get; set; } = new decimal?[10];
    public string? FileName { get; set; }
    public string? UserRemarks { get; set; }
    public string? SendToDoe { get; set; }
    public string? ApprovedByDoe { get; set; }
    public string DoeApprovalStatusDisplay { get; set; } = "Pending";
    public string? ReapprovalByDoe { get; set; }
    public int? ApproveUserId { get; set; }
    public DateTime? ApproveDate { get; set; }
    public string? DoeRemarks { get; set; }
    public decimal?[] Actuals { get; set; } = new decimal?[10];
    public DateTime? ActualsEntryDate { get; set; }
    public int? PrevRowId { get; set; }
    public string? Is16Fc { get; set; }
    public string? AppraisalAuth { get; set; }
    public int? AppraisalAuthorityId { get; set; }
    public string? WhetherAppraised { get; set; }
    public string? AppraisalStatusRemarks { get; set; }
    public string? IsApproved { get; set; }
    public string? NotApprovedRem { get; set; }
    public string? SchemeEndYear { get; set; }

    /// <summary>Convenience for the "clearly identify re-approval cases" DataForReapproval requirement.</summary>
    public bool IsReapprovalCase => string.Equals(ReapprovalByDoe, "Y", StringComparison.OrdinalIgnoreCase);
}

public class SaveSchemeOutlayRequestDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int DemandId { get; set; }
    public int SchemeId { get; set; }
    public string? ApproveAuth { get; set; }
    public decimal? TotalOutlay { get; set; }
    public decimal? CentralShare { get; set; }
    public decimal?[] Outlay { get; set; } = new decimal?[10];
    public string? UserRemarks { get; set; }
    public string? Is16Fc { get; set; }
    public string? AppraisalAuth { get; set; }
    public string? WhetherAppraised { get; set; }
    public string? AppraisalStatusRemarks { get; set; }
    public string? IsApproved { get; set; }
    public string? NotApprovedRem { get; set; }
    public string? SchemeEndYear { get; set; }

    /// <summary>Server-generated filename returned by UploadDocument — carried back on Save so the
    /// row picks up the attachment. Added 2026-08-18: was missing entirely, so a Save never
    /// persisted the uploaded PDF onto the row despite the upload step itself succeeding.</summary>
    public string? FileName { get; set; }
}

public class DoeApprovalActionRequestDto
{
    public string? DoeRemarks { get; set; }
}

public class EclActualsRequestDto
{
    public decimal?[] Actuals { get; set; } = new decimal?[10];
}

public class EclCategoryDto
{
    public int CategoryId { get; set; }
    public string? SerialNo { get; set; }
    public string? CategoryName { get; set; }
}

public class EclSchemeDto
{
    public int SchemeId { get; set; }
    public int DemandId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? SchemeSrNo { get; set; }
    public int? UmbSchemeId { get; set; }
}

/// <summary>Umbrella-scheme option from dbo.M_UmbScheme — a distinct shape from EclSchemeDto since
/// M_UmbScheme is a separate master table, not the same rows as dbo.M_Scheme.</summary>
public class EclUmbSchemeDto
{
    public int UmbSchemeId { get; set; }
    public string? UmSchemeName { get; set; }
    public int? DisplaySeqNo { get; set; }
}

public class EclApprovalAuthorityDto
{
    public int AuthorityId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public int? DisplaySequenceNo { get; set; }
}

/// <summary>Independent from EclApprovalAuthorityDto — "Appraisal Authority" and "Approval Authority" are two separate master tables/dropdowns (client request 2026-08-25), not a shared list.</summary>
public class EclAppraiseAuthorityDto
{
    public int AppraiseId { get; set; }
    public string AppraiseName { get; set; } = string.Empty;
    public int? DisplaySequenceNo { get; set; }
}

/// <summary>Request body for creating a new dbo.M_Scheme row from the ECL Master "Add Schemes" screen.</summary>
public class CreateSchemeRequestDto
{
    public int DemandId { get; set; }
    public int CategoryId { get; set; }
    public bool IsUmbrella { get; set; }
    public int? UmbrellaSchemeId { get; set; }
    public string SchemeNameEnglish { get; set; } = string.Empty;
    public string? SchemeNameHindi { get; set; }
}

public class EclFinancialYearOptionsDto
{
    public List<string> FinancialYears { get; set; } = new();
}

public class EclDocumentUploadResultDto
{
    public string StoredFileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long SizeBytes { get; set; }
}
