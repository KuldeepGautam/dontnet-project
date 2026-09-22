namespace UBIS.Services.Ecl.Infrastructure.Security;

using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>Validates the JWTs issued by AIM — ECL never issues its own tokens, only validates AIM's with the same shared SecretKey/Issuer/Audience.</summary>
public static class JwtValidationFactory
{
    public static TokenValidationParameters CreateValidationParameters(JwtOptions options) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
