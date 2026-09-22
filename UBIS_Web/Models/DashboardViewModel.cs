namespace UBIS.Web.Models;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;

    // -- Section 4 "Compliance Warning Engine" (added 2026-07-10): non-blocking staleness nudge.
    public bool ShowComplianceWarning { get; set; }

    public bool PasswordIsStale { get; set; }

    public bool EmailIsStale { get; set; }
}
