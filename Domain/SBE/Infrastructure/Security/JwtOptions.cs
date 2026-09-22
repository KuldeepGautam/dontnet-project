namespace UBIS.Services.Sbe.Infrastructure.Security;

/// <summary>
/// Binds to "SecurityConfiguration:Jwt" in appsettings.json. SBE only ever validates tokens here
/// (never issues one) — SecretKey/Issuer/Audience must match AIM's actual runtime values exactly,
/// since AIM is the sole issuer.
/// </summary>
public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
