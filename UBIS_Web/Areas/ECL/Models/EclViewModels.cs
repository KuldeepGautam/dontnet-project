#nullable enable
namespace UBIS.Web.Areas.ECL.Models;

using UBIS.Web.Services.Clients;

/// <summary>View models for the ECL area (scheme-outlay entry, DOE approval, reports). Added 2026-08-18 (Part 2).</summary>
public class EclEntryViewModel
{
    public int DemandId { get; set; }
    public int DemandNo { get; set; }
    
    public string DemandName { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;

    public List<EclCategoryDto> Categories { get; set; } = new();

    public int? SelectedCategoryId { get; set; }

    public List<EclSchemeDto> Schemes { get; set; } = new();

    public int? SelectedSchemeId { get; set; }

    public List<EclApprovalAuthorityDto> ApprovalAuthorities { get; set; } = new();

    /// <summary>Independent from ApprovalAuthorities above — "Appraisal Authority" and "Approval Authority" are two separate master tables/dropdowns (client request 2026-08-25).</summary>
    public List<EclAppraiseAuthorityDto> AppraiseAuthorities { get; set; } = new();

    public List<string> SchemeEndYearOptions { get; set; } = new();

    /// <summary>All 10 selectable financial years starting at ECL_Config.ECL_StartYear — used both
    /// for the outlay column headers and to compute which SchemeEndYear-gated columns stay editable.</summary>
    public List<string> OutlayYearColumns { get; set; } = new();

    public List<EclSchemeOutlayDto> SavedRows { get; set; } = new();

    /// <summary>Set when this GET was reached via the saved grid's Edit column (?rowId=X) — the
    /// entry form above pre-populates from this instead of rendering blank, and its RowId hidden
    /// field is set so POST calls UpdateOutlayAsync instead of CreateOutlayAsync. Added 2026-08-18.</summary>
    public EclSchemeOutlayDto? SelectedOutlay { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}

public class EclActualsViewModel
{
    public int DemandId { get; set; }

    public string DemandName { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;

    public List<EclCategoryDto> Categories { get; set; } = new();

    public int? SelectedCategoryId { get; set; }

    public List<EclSchemeDto> Schemes { get; set; } = new();

    public int? SelectedRowId { get; set; }

    public EclSchemeOutlayDto? SelectedOutlay { get; set; }

    public List<EclSchemeOutlayDto> SubmittedRows { get; set; } = new();

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}

public class EclSchemeMasterViewModel
{
    public List<UBIS.Web.Services.Clients.DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public List<EclCategoryDto> Categories { get; set; } = new();

    public int? SelectedCategoryId { get; set; }

    public List<EclUmbSchemeDto> UmbrellaSchemeOptions { get; set; } = new();

    public string FinancialYear { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}

/// <summary>Bound directly from AddSchemeOutlay's &lt;form&gt; POST. Field names match the Razor
/// view's asp-for names 1:1. RowId is null/0 on create, set on an edit-and-resave from the saved grid.</summary>
public class EclOutlayFormModel
{
    public int? RowId { get; set; }

    public int DemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int SchemeId { get; set; }

    public string? Is16Fc { get; set; }

    public decimal? TotalOutlay { get; set; }

    public decimal? CentralShare { get; set; }

    /// <summary>"No" case only — bound from the form but currently NOT forwarded anywhere: its
    /// former persistence target, ECL_T_Outlay.InitialTotalOutlay, was dropped 2026-08-18
    /// (ecl-workstream-4-outlay-column-cleanup.sql) since the legacy app that needed it is retired.
    /// No replacement column was requested, so this field is a known no-op pending a decision on
    /// where (if anywhere) it should be re-homed.</summary>
    public decimal? RemainingOutlay { get; set; }

    public string? SchemeEndYear { get; set; }

    public string? AppraisalAuth { get; set; }

    public string? AppraisalAuthOther { get; set; }

    public string? WhetherAppraised { get; set; }

    public string? AppraisalStatusRemarks { get; set; }

    public string? ApproveAuth { get; set; }

    public string? ApproveAuthOther { get; set; }

    public string? IsApproved { get; set; }

    public string? NotApprovedRem { get; set; }

    public decimal?[] Outlay { get; set; } = new decimal?[10];

    public string? UserRemarks { get; set; }

    public string? FileName { get; set; }
}

public class EclApprovalGridViewModel
{
    public string Title { get; set; } = string.Empty;

    public List<UBIS.Web.Services.Clients.DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public List<string> FinancialYearOptions { get; set; } = new();

    public List<EclSchemeOutlayDto> Rows { get; set; } = new();

    /// <summary>Column headers for the 10-year outlay grid, aligned to Rows[i].Outlay index order.</summary>
    public List<string> OutlayYearColumns { get; set; } = new();

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }

    /// <summary>True only when StatusMessage is the result of an actual save-type action
    /// (Approve/Reject/ApproveSelected/RejectSelected/Reapprove POST), false for a plain GET page
    /// load's own status (e.g. "Could not load rows." when GetOutlaysAsync fails). Bug report
    /// 2026-08-27: ecl.js's global initSaveStatusDialog() pops a "Record Saved"/"Could not save"
    /// dialog for ANY status paragraph it finds with role="status", with no way to tell a save
    /// result apart from a load failure - a GET that merely failed to LOAD rows was being announced
    /// as "Could not save" (implying a save was attempted, which it wasn't). The view only renders
    /// role="status" (the marker that selector keys on) when this is true.</summary>
    public bool IsSaveResult { get; set; }

    public bool RejectionRequiresRemarks { get; set; } = true;
}

public class EclReportViewModel
{
    public string Title { get; set; } = string.Empty;

    public List<UBIS.Web.Services.Clients.DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public List<EclCategoryDto> Categories { get; set; } = new();

    public int? SelectedCategoryId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public List<string> FinancialYearOptions { get; set; } = new();

    public List<EclSchemeOutlayDto> Rows { get; set; } = new();

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }

    /// <summary>True when this report is being served off EclOutlayController's general list
    /// endpoint rather than a purpose-built reporting endpoint (Part 1 built none) — surfaced in the
    /// view as a small note, not hidden, per the brief's "note if a purpose-built endpoint would be
    /// better long-term" instruction.</summary>
    public bool UsesGeneralListEndpoint { get; set; } = true;

    /// <summary>Only DataAnalysis's mockup (ECLReport.html) shows a Category filter — the three
    /// demand-wise/pending-demand reports filter by Demand/FY only.</summary>
    public bool ShowCategoryFilter { get; set; }
}
