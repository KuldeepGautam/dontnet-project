namespace UBIS.Web.Configuration;

/// <summary>Binds to "AuthCookie". This cookie only ever carries an opaque identity, never the AIM JWT.</summary>
public class AuthCookieOptions
{
    public string Name { get; set; } = ".UBIS.Auth";

    public int ExpireMinutes { get; set; } = 20;
}
