namespace UBIS.ApiGateway.Logging;

using Microsoft.Extensions.Options;

/// <summary>
/// Global Ocelot delegating handler (registered via AddDelegatingHandler&lt;T&gt;(global: true) in
/// Program.cs), two jobs on every proxied request:
///   1. Adds the internal-caller shared-secret header AIM/MenuGenerator's CallerRestrictionMiddleware
///      expects — today UBIS_Web sends this directly; once the Gateway is the caller, it has to.
///   2. Logs to LogWriter (via GatewayLogWriterClient) when a downstream call ultimately fails.
///
/// Known open question (flagged in UBIS_Web/PAGE_SCAFFOLDING_PLAN.claude §4 step 6, not silently
/// assumed solved): whether Ocelot positions global delegating handlers inside or outside its own
/// Polly/QoS retry wrapper isn't confirmed against this installed Ocelot version. If this handler
/// runs inside the retry wrapper, it'll see — and log — every individual retry attempt's failure,
/// not just the final one after all 5 are exhausted, meaning "log once when it finally fails" could
/// become "log up to 5 times." Functionally harmless (LogWriter just gets extra entries), but worth
/// verifying empirically once this is running end-to-end and tightening (e.g. only log on the
/// specific exception type Polly throws once its circuit breaker opens, BrokenCircuitException)
/// if it turns out to over-log in practice.
/// </summary>
public class GatewayForwardingHandler : DelegatingHandler
{
    private readonly InternalCallerOptions _options;
    private readonly GatewayLogWriterClient _logWriter;

    public GatewayForwardingHandler(IOptions<InternalCallerOptions> options, GatewayLogWriterClient logWriter)
    {
        _options = options.Value;
        _logWriter = logWriter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.SharedKey) && !request.Headers.Contains(_options.HeaderName))
        {
            request.Headers.Add(_options.HeaderName, _options.SharedKey);
        }

        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync(
                $"Downstream call failed: {request.Method} {request.RequestUri}",
                ex,
                new { request.Method, Uri = request.RequestUri?.ToString() },
                CancellationToken.None // deliberate: don't let a caller-cancelled request also cancel the log write
            ).ConfigureAwait(false);
            throw; // never swallow — Ocelot's own error handling/response-to-caller still needs to see this
        }
    }
}

/// <summary>Matches UBIS_Web's InternalCaller config shape (HeaderName/SharedKey) — same shared secret every service already checks.</summary>
public class InternalCallerOptions
{
    public string HeaderName { get; set; } = "X-UBIS-Internal-Client-Key";
    public string SharedKey { get; set; } = string.Empty;
}
