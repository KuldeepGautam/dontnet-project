namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

/// <summary>Backs the Select-Demand-and-Appendix screen (replaces the old app's SelectAppendix.aspx / sp_Select_Demands_Template).</summary>
public class PreBudgetSelectorViewModel
{
    public List<DemandDto> Demands { get; set; } = new();

    public int? SelectedDemandId { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Populated only once a Demand is selected — each Appendix's current submission status for that Demand.</summary>
    public List<PreBudgetAppendixWithStatusDto> Appendices { get; set; } = new();

    public string? SelectedAppendixCode { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
