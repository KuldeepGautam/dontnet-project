namespace UBIS.Web.Models.Admin;

using UBIS.Web.Services.Clients;

/// <summary>Active Session Monitor admin screen. Added 2026-08-10 (Session Management/Redis Cache test case).</summary>
public class ActiveSessionsViewModel
{
    public List<ActiveSessionDto> Sessions { get; set; } = new();

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
