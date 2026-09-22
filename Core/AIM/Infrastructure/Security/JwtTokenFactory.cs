namespace UBIS.Services.Aim.Infrastructure.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Issues and validates the signed JWTs used across AIM and every service/client that trusts it.
/// Replaces the previous Base64-JSON placeholder token.
/// </summary>
public static class JwtTokenFactory
{
    public static SymmetricSecurityKey CreateSigningKey(JwtOptions options) =>
        new(Encoding.UTF8.GetBytes(options.SecretKey));

    public static string CreateToken(JwtOptions options, IEnumerable<Claim> claims, DateTime expiresAtUtc)
    {
        var credentials = new SigningCredentials(CreateSigningKey(options), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(options),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
