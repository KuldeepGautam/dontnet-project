namespace UBIS.Web.Controllers.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Models;
using UBIS.Web.Services.Caching;
using UBIS.Web.Services.Session;

/// <summary>
/// Server-side (Redis-backed) tier of the data-entry autosave feature — see
/// wwwroot/js/autosave.js for the client-side localStorage tier and restore-banner UX.
/// Together they fix the old system's data-loss problem on long (5–10 minute) entry pages:
/// a forced sign-out or a network drop never loses what the user typed.
/// </summary>
[ApiController]
[Route("api/draft")]
[Authorize]
public class DraftController : ControllerBase
{
    private readonly IAppCacheService _cache;
    private readonly TimeSpan _ttl;

    public DraftController(IAppCacheService cache, IConfiguration configuration)
    {
        _cache = cache;
        _ttl = TimeSpan.FromHours(configuration.GetValue<int?>("Draft:TtlHours") ?? 48);
    }

    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] SaveDraftRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest();
        }

        var entry = new DraftEntry { DataJson = request.DataJson, SavedAtUtc = DateTime.UtcNow };
        await _cache.SetAsync(BuildKey(request.Key), entry, _ttl, ct);
        return Ok(new { savedAtUtc = entry.SavedAtUtc });
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest();
        }

        var entry = await _cache.GetAsync<DraftEntry>(BuildKey(key), ct);
        return entry == null ? NotFound() : Ok(entry);
    }

    [HttpDelete("{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string key, CancellationToken ct)
    {
        await _cache.RemoveAsync(BuildKey(key), ct);
        return NoContent();
    }

    /// <summary>Scoped by FinancialYear (added 2026-08-18) as well as username - without it, a
    /// user who switches financial year mid-session (same Appendix+Demand key, e.g.
    /// "PreBudget-AppendixI-42") would have last year's in-progress draft silently restored into,
    /// or overwritten by, this year's form.</summary>
    private string BuildKey(string key)
    {
        var financialYear = HttpContext.Session.GetUbisSession()?.FinancialYear ?? "";
        return $"draft:{User.Identity?.Name}:{financialYear}:{key}";
    }
}
