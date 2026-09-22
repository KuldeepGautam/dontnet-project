using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using UBIS.ApiGateway;
using UBIS.ApiGateway.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// MicroserviceSettings.json (repo root, copied to output as a linked file — see the .csproj) is
// the single source of truth for where each service actually is; this overrides just the
// DownstreamHostAndPorts values ocelot.json's Routes already declare, added after ocelot.json's
// own AddJsonFile so it wins for the same keys, but before AddOcelot(builder.Configuration) reads
// the merged result. See UBIS_Web/PAGE_SCAFFOLDING_PLAN.claude §4 step 2 for the full rationale.
var microserviceSettingsOverrides = MicroserviceSettingsLoader.BuildOverrides(
    Path.Combine(builder.Environment.ContentRootPath, "MicroserviceSettings.json"),
    Path.Combine(builder.Environment.ContentRootPath, "ocelot.json"));
builder.Configuration.AddInMemoryCollection(microserviceSettingsOverrides!);

builder.Services.Configure<InternalCallerOptions>(builder.Configuration.GetSection("InternalCaller"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<GatewayLogWriterClient>();
builder.Services.AddTransient<GatewayForwardingHandler>();

builder.Services.AddOcelot(builder.Configuration)
    .AddPolly()
    .AddDelegatingHandler<GatewayForwardingHandler>(global: true);

// JWT validation matches AIM's JwtTokenFactory.CreateValidationParameters exactly (same
// SecretKey/Issuer/Audience — tokens are issued by AIM, the Gateway must validate with the
// same key). Wired into per-route AuthenticationOptions in ocelot.json (2026-07-21) — every route
// was audited action-by-action against AIM's/MenuGenerator's real [Authorize]/[AllowAnonymous]
// attributes (see the "_comment" on each ocelot.json route for the specific finding), not guessed.
// Downstream [Authorize] on AIM's/MenuGenerator's own controllers is unchanged and still enforces
// auth regardless — this is additive, not a replacement.
var jwtSection = builder.Configuration.GetSection("SecurityConfiguration:Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Ocelot's rate limiter (25 req/sec, ocelot.json's per-route RateLimitOptions) always requires a
// client-identifying header to key its counters on — confirmed empirically (it does NOT fall back
// to remote IP automatically in this Ocelot version, tried without this middleware first and every
// request 503'd with "client ID header required"). Stamping one here means no caller (UBIS_Web,
// Postman, MenuScaffolder, etc.) needs to know this header exists — defaults to remote IP, which
// gives every distinct machine its own 25/sec budget rather than one shared global budget.
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("ClientId"))
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        context.Request.Headers["ClientId"] = clientId;
    }

    await next();
});

await app.UseOcelot();

app.Run();
