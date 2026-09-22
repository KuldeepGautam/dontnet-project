namespace UBIS.Web.Services.Clients;

using Microsoft.Extensions.Options;
using UBIS.Web.Configuration;

/// <summary>
/// Adds the shared-secret header AIM's and MenuGenerator's CallerRestrictionMiddleware require
/// on every request, so UBIS_Web is recognized as a trusted internal caller.
/// </summary>
public class InternalCallerHandler : DelegatingHandler
{
    private readonly InternalCallerOptions _options;

    public InternalCallerHandler(IOptions<InternalCallerOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.SharedKey))
        {
            request.Headers.Remove(_options.HeaderName);
            request.Headers.Add(_options.HeaderName, _options.SharedKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
