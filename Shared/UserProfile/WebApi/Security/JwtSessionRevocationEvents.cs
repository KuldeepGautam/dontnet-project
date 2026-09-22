namespace UBIS.Services.UserProfile.WebApi.Security;

using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;

/// <summary>
/// FR-006 (Token Refresh &amp; Revocation), added 2026-08-21: a cryptographically valid, unexpired
/// JWT is still rejected once its session's <c>ubis:session:{sid}</c> Redis key is gone — deleted by
/// AIM's force-logout, by a password change revoking every outstanding session, or simply expired
/// via its own sliding TTL. Every JWT-validating microservice registers this same events object
/// (duplicated per service, matching how JwtOptions/JwtValidationFactory are already duplicated
/// rather than shared) so a revoked token is rejected everywhere, not just by AIM itself.
/// Lives in the WebApi project, not Infrastructure — JwtBearerEvents requires the
/// Microsoft.AspNetCore.Authentication.JwtBearer package, only referenced here.
/// </summary>
public static class JwtSessionRevocationEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnTokenValidated = async context =>
        {
            var sessionId = context.Principal?.FindFirst("sid")?.Value;
            if (string.IsNullOrEmpty(sessionId))
            {
                context.Fail("Token is missing a session identifier.");
                return;
            }

            // Fails OPEN (allows the request through, same as every other Redis/RabbitMQ client in
            // this solution tolerating a down broker at call-time) rather than closed if Redis
            // itself is unreachable - a revocation-list check that can take the entire application
            // down whenever Redis has a hiccup is a worse outcome than briefly not enforcing
            // revocation. Confirmed real-world failure mode 2026-08-21: an unhandled
            // RedisConnectionException here previously broke every authenticated request to every
            // service that had this check, the moment Redis was unreachable from that host.
            try
            {
                var redis = context.HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();
                var sessionKey = $"ubis:session:{sessionId}";
                var raw = await db.StringGetAsync(sessionKey);
                if (!raw.HasValue)
                {
                    context.Fail("Session has been revoked or has expired.");
                    return;
                }

                // Bug fix 2026-08-27 (tester report: "logged out automatically after 15 minutes...
                // then after logging in again, got logged out again in between doing work"): this
                // check only ever READ the session key - nothing on the request path ever refreshed
                // its sliding TTL (AIM's CacheService.GetAsync does this correctly for its own
                // callers, but this raw Redis check bypasses CacheService entirely), so the session
                // hard-expired exactly ExpirationMinutes after login/last refresh regardless of how
                // actively the user kept working. Re-applies the session's own SlideSeconds (stored
                // in the envelope by CacheService.SetAsync at login) on every validated request
                // instead, so the session genuinely slides while in use - same fix needed in every
                // JWT-validating microservice's copy of this file (duplicated per service, see the
                // class doc comment).
                try
                {
                    using var envelope = JsonDocument.Parse((string)raw!);
                    if (envelope.RootElement.TryGetProperty("SlideSeconds", out var slideSeconds) && slideSeconds.ValueKind == JsonValueKind.Number)
                    {
                        await db.KeyExpireAsync(sessionKey, TimeSpan.FromSeconds(slideSeconds.GetDouble()));
                    }
                }
                catch (JsonException)
                {
                    // Malformed/unexpected envelope shape - the session is still valid for this
                    // request, just skip the slide-refresh rather than failing the whole check.
                }
            }
            catch (RedisException ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtSessionRevocationEvents");
                logger.LogWarning(ex, "Could not reach Redis to check session revocation for sid {SessionId} - allowing the request through.", sessionId);
            }
        }
    };
}
