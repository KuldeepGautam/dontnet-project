namespace UBIS.Web.Models.Admin;

using UBIS.Web.Services.Clients;

/// <summary>Admin pending Mobile/IP change-request review screen. Added 2026-07.</summary>
public class PendingRequestsViewModel
{
    public List<PendingChangeRequestDto> Items { get; set; } = new();

    public string? StatusMessage { get; set; }

    public bool StatusIsError { get; set; }
}
