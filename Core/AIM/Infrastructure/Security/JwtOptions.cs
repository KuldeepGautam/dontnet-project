namespace UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Binds to "SecurityConfiguration:Jwt" in appsettings.json.
/// </summary>
public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
