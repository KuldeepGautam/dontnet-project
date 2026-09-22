namespace UBIS.Web.Models.Admin;

using System.ComponentModel.DataAnnotations;

/// <summary>Admin-configurable session countdown (added 2026-07 — client MOM asked for this to be
/// changeable "from the Admin Panel" rather than only via appsettings.json/redeploy).</summary>
public class SessionSettingsViewModel
{
    [Range(1, 120, ErrorMessage = "Enter a value between 1 and 120 minutes.")]
    [Display(Name = "Session Countdown (minutes)")]
    public int CurrentCountdownMinutes { get; set; }

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
