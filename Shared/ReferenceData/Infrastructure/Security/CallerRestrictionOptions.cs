namespace UBIS.Services.ReferenceData.Infrastructure.Security;

/// <summary>Enabled moved to dbo.AppSettings.EnableCallerRestriction (2026-08-07) - see CallerRestrictionMiddleware/AppSettingsReader.</summary>
public class CallerRestrictionOptions
{
    public List<string> AllowedCallerHostHeaderValues { get; set; } = new();
    public string SharedClientKeyHeaderName { get; set; } = "X-UBIS-Internal-Client-Key";
    public string SharedClientKey { get; set; } = string.Empty;
}
