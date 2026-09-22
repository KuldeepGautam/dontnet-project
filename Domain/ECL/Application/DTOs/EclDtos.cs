namespace UBIS.Services.Ecl.Application.DTOs;

/// <summary>Full read-model DTO for an ECL scheme outlay row, returned by GET endpoints.</summary>
public class EclSchemeOutlayDto
{
    public int RowId { get; set; }
    public string? FinancialYear { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? DemandId { get; set; }
    public int? SchemeId { get; set; }

    /// <summary>Resolved server-side (added 2026-08-19) so the saved-outlay grid shows the real
    /// name even for rows outside whatever category is currently selected in the entry form.</summary>
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
}

/// <summary>Request body to create/update an ECL scheme outlay's editable fields (entry screen).</summary>
public class SaveSchemeOutlayRequestDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int DemandId { get; set; }
    public int SchemeId { get; set; }

    /// <summary>Either a M_ECLApprovalAuthority.AuthorityName or a free-typed "Others" value — resolved/promoted server-side to ApprovalAuthorityId on save (see EclOutlayRepository.EnsureApprovalAuthorityAsync).</summary>
    public string? ApproveAuth { get; set; }

    public decimal? TotalOutlay { get; set; }

    /// <summary>Rupee amount — CentralSharePercentage is always recomputed server-side from this and TotalOutlay, a client-supplied percentage is never trusted.</summary>
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

    /// <summary>Server-generated filename returned by POST /upload-document (EclOutlayController.UploadDocument) — the client attaches it here after a successful upload so it lands on the row alongside everything else. Never a client-chosen name.</summary>
    public string? FileName { get; set; }
}

/// <summary>Request body for the DOE approve/reject actions.</summary>
public class DoeApprovalActionRequestDto
{
    public string? DoeRemarks { get; set; }
}

/// <summary>Request body to record the FY1-10 actuals grid.</summary>
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

/// <summary>Request body for creating a new dbo.M_Scheme row from the ECL Master "Add Schemes" screen.</summary>
public class CreateSchemeRequestDto
{
    public int DemandId { get; set; }
    public int CategoryId { get; set; }
    public bool IsUmbrella { get; set; }

    /// <summary>Only meaningful when IsUmbrella is false — FK to dbo.M_UmbScheme.UmbSchemeID (the
    /// real umbrella-scheme master table, added 2026-08-19) for the umbrella scheme this one nests
    /// under. Was previously accepted here but silently never persisted anywhere.</summary>
    public int? UmbrellaSchemeId { get; set; }

    public string SchemeNameEnglish { get; set; } = string.Empty;
    public string? SchemeNameHindi { get; set; }
}

public class EclApprovalAuthorityDto
{
    public int AuthorityId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public int? DisplaySequenceNo { get; set; }
}

/// <summary>Independent from EclApprovalAuthorityDto — the "Appraisal Authority" and "Approval Authority" dropdowns are backed by two separate master tables (client request 2026-08-25), not a shared list.</summary>
public class EclAppraiseAuthorityDto
{
    public int AppraiseId { get; set; }
    public string AppraiseName { get; set; } = string.Empty;
    public int? DisplaySequenceNo { get; set; }
}

/// <summary>The 10 selectable financial years, generated from ECL_Config.ECL_StartYear.</summary>
public class EclFinancialYearOptionsDto
{
    public List<string> FinancialYears { get; set; } = new();
}

/// <summary>Response after a successful PDF upload — the caller then supplies this FileName back on save.</summary>
public class EclDocumentUploadResultDto
{
    public string StoredFileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long SizeBytes { get; set; }
}
