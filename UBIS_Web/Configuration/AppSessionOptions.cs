namespace UBIS.Web.Configuration;

/// <summary>Binds to "SessionOptions" (named to avoid clashing with ASP.NET Core's own SessionOptions).</summary>
public class AppSessionOptions
{
    public int IdleTimeoutMinutes { get; set; } = 20;

    public string CookieName { get; set; } = ".UBIS.Session";
}
