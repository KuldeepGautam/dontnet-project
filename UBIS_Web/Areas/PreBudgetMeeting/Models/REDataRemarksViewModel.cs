namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>FR-003: RE Data Remarks. Server-side role gate is authoritative (RemarksController); CanPostRemark is a UI convenience only.</summary>
public class REDataRemarksViewModel
{
    public List<DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public List<PreBudgetAppendixDto> Appendices { get; set; } = new();

    public int? SelectedAppendixId { get; set; }

    public List<PreBudgetRemarkDto> Remarks { get; set; } = new();

    public bool CanPostRemark { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
