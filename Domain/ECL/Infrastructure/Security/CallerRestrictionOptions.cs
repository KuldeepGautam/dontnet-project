namespace UBIS.Services.Ecl.Infrastructure.Security;

/// <summary>Enabled/disabled via dbo.AppSettings.EnableCallerRestriction and DeveloperEnv — see CallerRestrictionMiddleware/AppSettingsReader.</summary>
public class CallerRestrictionOptions
{
    public List<string> AllowedCallerHostHeaderValues { get; set; } = new();
    public string SharedClientKeyHeaderName { get; set; } = "X-UBIS-Internal-Client-Key";
    public string SharedClientKey { get; set; } = string.Empty;
}
