namespace UBIS.Services.MenuGenerator.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using UBIS.Services.MenuGenerator.Infrastructure.Configuration;

/// <summary>
/// Restricts calls to this service to whoever holds the shared internal key (i.e. the MVC app).
/// This is the only auth layer MenuGenerator has - it does not validate JWTs itself.
/// </summary>
public class CallerRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CallerRestrictionOptions _options;
    private readonly AppSettingsReader _appSettingsReader;

    public CallerRestrictionMiddleware(
        RequestDelegate next,
        IOptions<CallerRestrictionOptions> options,
        AppSettingsReader appSettingsReader)
    {
        _next = next;
        _options = options.Value;
        _appSettingsReader = appSettingsReader;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skipped entirely while a demo runs on the developer's own machine - dbo.AppSettings.
        // DeveloperEnv (renamed from SecurityConfiguration:IsDevelopmentTime 2026-08-07). Flip to 0
        // on the real server to start enforcing (independent of the more granular
        // EnableCallerRestriction below, which stays available for finer control once that's the
        // active gate).
        if (await _appSettingsReader.GetBoolAsync("DeveloperEnv", defaultValue: true))
        {
            await _next(context);
            return;
        }

        if (!await _appSettingsReader.GetBoolAsync("EnableCallerRestriction", defaultValue: true))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(_options.SharedClientKeyHeaderName, out var providedKey))
        {
            await WriteForbiddenAsync(context, "Caller not authorized.");
            return;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedKey.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(_options.SharedClientKey);
        if (!CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            await WriteForbiddenAsync(context, "Caller not authorized.");
            return;
        }

        var declaredOrigin = context.Request.Headers["X-UBIS-Caller-Host"].ToString();
        if (!string.IsNullOrEmpty(declaredOrigin) && !_options.AllowedCallerHostHeaderValues.Contains(declaredOrigin, StringComparer.OrdinalIgnoreCase))
        {
            await WriteForbiddenAsync(context, "Caller host not recognized.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string error)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error }));
    }
}
