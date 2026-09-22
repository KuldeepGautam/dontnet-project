namespace UBIS.Web.Configuration;

/// <summary>
/// Binds to "InternalCaller". Value is sent on every server-to-server call to AIM/MenuGenerator,
/// which both reject requests missing this header via their CallerRestrictionMiddleware.
/// </summary>
public class InternalCallerOptions
{
    public string HeaderName { get; set; } = "X-UBIS-Internal-Client-Key";

    public string SharedKey { get; set; } = string.Empty;
}
