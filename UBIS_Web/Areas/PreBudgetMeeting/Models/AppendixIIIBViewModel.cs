namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>Appendix III-B: Status of Scheme Appraisal/Approval during the XVI Finance Commission
/// Cycle. New appendix, 2026-09-09 - see Appendix_Papes_I_to IIIB/Appendix-III-B.html.</summary>
public class AppendixIIIBViewModel : AppendixBaseViewModel
{
    public List<AppendixSchemeAppraisalStatusDto> Records { get; set; } = new();
    public SaveAppendixSchemeAppraisalStatusDto NewRecord { get; set; } = new();

    /// <summary>Category dropdown options - dbo.M_Category for the current FY with SerialNo II/IV
    /// (client instruction 2026-09-10). Scheme cascades from the chosen Category via AJAX
    /// (GetAppendixIIIBSchemes).</summary>
    public List<AppendixIIIBCategoryOptionDto> Categories { get; set; } = new();
}
