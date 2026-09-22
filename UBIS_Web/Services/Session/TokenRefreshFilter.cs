namespace UBIS.Web.Services.Session;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Filters;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Logging;

/// <summary>
/// Runs on every request; if the AIM JWT stashed in Session is close to expiring, silently
/// exchanges the refresh token for a new one so a long data-entry form never gets interrupted.
/// If the refresh call itself fails, the request proceeds with the old token — the next
/// microservice call will 401 and the user is naturally sent back to Login.
/// </summary>
public class TokenRefreshFilter : IAsyncActionFilter
{
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(2);

    // Bug fix 2026-09-03 ("page still refreshes dropdown of demand or appendix"): traced via
    // dbo.M_LogEntry to repeated "Refresh token is invalid or has expired" 401s from AIM, all on
    // session-monitor.js's three 5-second polls (heartbeat, change-request-status,
    // security-stamp-status) - each is an independent [Authorize] request that runs this filter,
    // so when the access token is near expiry, all three fire the refresh exchange nearly
    // simultaneously with the SAME (single-use) refresh token. AIM invalidates a refresh token the
    // moment it's redeemed, so only the first of the three succeeds; the other two get rejected
    // with exactly that error, one of them proceeds on a now-broken session, and the next real
    // page load 401s - which (since the long-lived auth cookie is still valid) bounces through
    // Login straight back to Dashboard invisibly, reading as "the page just refreshed" and
    // discarding whatever Demand/Appendix was selected client-side. Fixed by serializing the
    // refresh exchange per session: only the first concurrent request actually calls AIM; the
    // others reuse its result from this short-lived cache instead of redeeming the same token twice.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();
    private static readonly ConcurrentDictionary<string, RefreshedToken> _recentlyRefreshed = new();
    private static readonly TimeSpan RecentRefreshTtl = TimeSpan.FromSeconds(10);

    private readonly IAimClient _aimClient;
    private readonly IWebLogClient _logClient;

    public TokenRefreshFilter(IAimClient aimClient, IWebLogClient logClient)
    {
        _aimClient = aimClient;
        _logClient = logClient;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var session = httpContext.Session;
            var data = session.GetUbisSession();

            if (data != null
                && !string.IsNullOrEmpty(data.RefreshToken)
                && data.TokenExpiresAtUtc - DateTime.UtcNow < RefreshThreshold)
            {
                var sessionId = session.Id;

                if (TryApplyRecentRefresh(sessionId, data, session))
                {
                    await next();
                    return;
                }

                var gate = _refreshLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
                try
                {
                    await gate.WaitAsync(httpContext.RequestAborted);
                    try
                    {
                        // Another request for this same session may have redeemed the refresh token
                        // and populated the cache while we were waiting for the gate.
                        if (!TryApplyRecentRefresh(sessionId, data, session))
                        {
                            var result = await _aimClient.RefreshAsync(data.RefreshToken, httpContext.RequestAborted);
                            if (result.IsSuccess && result.Data != null)
                            {
                                data.Token = result.Data.Token;
                                data.RefreshToken = result.Data.RefreshToken;
                                data.TokenExpiresAtUtc = result.Data.TokenExpiresAt;
                                session.SetUbisSession(data);
                                _recentlyRefreshed[sessionId] = new RefreshedToken(data.Token, data.RefreshToken, data.TokenExpiresAtUtc, DateTime.UtcNow);
                            }
                            else
                            {
                                // Client report 2026-08-27: "page is refreshing automatically...
                                // appendix dropdown refreshed and input screens got closed
                                // automatically" - the outward symptom of exactly this branch (old
                                // comment above: "the next microservice call will 401 and the user is
                                // naturally sent back to Login"), but this branch itself never logged
                                // WHY the refresh failed, so every prior occurrence left no trail to
                                // diagnose from. Logged here now (path/session id, not the token
                                // itself) so the next occurrence is actually traceable instead of only
                                // visible as "another Login row showed up sooner than expected" in the
                                // log.
                                await _logClient.WarnAsync(
                                    "Silent token refresh failed - request proceeds on the old token and will 401 on the next microservice call.",
                                    new { path = httpContext.Request.Path.Value, statusCode = result.StatusCode, errorCode = result.Error?.Code, errorMessage = result.Error?.Message },
                                    httpContext.RequestAborted).ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        gate.Release();
                    }
                }
                catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
                {
                    // Bug fix 2026-09-17 (client report: unhandled TaskCanceledException here,
                    // surfacing as an intermittent forced logout/500) - RequestAborted fires
                    // whenever the CLIENT itself disconnects mid-request (tab closed, navigated
                    // away, a fetch() superseded by another) while this filter is waiting on
                    // gate.WaitAsync or the AIM refresh call - a routine occurrence given
                    // session-monitor.js's three independent 5-second polls, not a real failure.
                    // Previously unhandled, this crashed the whole action pipeline instead of
                    // simply abandoning a request nobody is waiting on anymore; there is nothing
                    // useful left to do for an already-cancelled request, so just let it end here
                    // rather than proceeding into the controller action (or rethrowing) for a
                    // response the client already gave up on.
                    return;
                }
            }
        }

        await next();
    }

    /// <summary>
    /// If another concurrent request for this session already redeemed the refresh token within
    /// the last few seconds, apply that result to this request's session data instead of racing
    /// to redeem the same (now-consumed) token again. Also opportunistically evicts this entry and
    /// its lock once stale, so the two dictionaries don't grow unbounded across the process's
    /// lifetime as sessions come and go.
    /// </summary>
    private static bool TryApplyRecentRefresh(string sessionId, UbisSessionData data, ISession session)
    {
        if (!_recentlyRefreshed.TryGetValue(sessionId, out var cached))
        {
            return false;
        }

        if (DateTime.UtcNow - cached.CachedAtUtc >= RecentRefreshTtl)
        {
            _recentlyRefreshed.TryRemove(sessionId, out _);
            _refreshLocks.TryRemove(sessionId, out _);
            return false;
        }

        data.Token = cached.Token;
        data.RefreshToken = cached.RefreshToken;
        data.TokenExpiresAtUtc = cached.ExpiresAtUtc;
        session.SetUbisSession(data);
        return true;
    }

    private readonly record struct RefreshedToken(string Token, string RefreshToken, DateTime ExpiresAtUtc, DateTime CachedAtUtc);
}
