using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.MenuGenerator.Application.Interfaces;
using UBIS.Services.MenuGenerator.Infrastructure.Caching;
using UBIS.Services.MenuGenerator.Infrastructure.Configuration;
using UBIS.Services.MenuGenerator.Infrastructure.Logging;
using UBIS.Services.MenuGenerator.Infrastructure.Messaging;
using UBIS.Services.MenuGenerator.Infrastructure.Persistence;
using UBIS.Services.MenuGenerator.Infrastructure.Security;
using UBIS.Services.MenuGenerator.Infrastructure.Services;
using UBIS.Services.MenuGenerator.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// DB-first as of 2026-07-10: no EF migrations anywhere in this solution, so no migrations
// history table is configured — schema is owned by Core/MenuGenerator/SQL/*.sql, DBA-reviewed.
builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// DB-backed feature flags (EnableEmail/EnableIPLogging/EnableHttps/EnableCertificate) - see
// dbo.AppSettings and AppSettingsReader's own doc comment for why this replaced the old
// Security:UseHttps/SecurityConfiguration:EnableUserIPSettings config reads.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.")));
// L1 (in-process) tier for the two-tier ICacheService below.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<ILogWriterClient, RabbitMqLogWriterClient>();

builder.Services.Configure<CallerRestrictionOptions>(builder.Configuration.GetSection("CallerRestriction"));

builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ISessionRoleResolver, SessionRoleResolver>();

// Validates the JWT AIM issued at login (never issues one here) — MapInboundClaims stays false
// so the "sid" claim keeps its short name exactly as AIM wrote it.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtValidationFactory.CreateValidationParameters(jwtOptions);
        options.Events = JwtSessionRevocationEvents.Create();
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "JWT issued by AIM's /api/authentication/login. Enter as: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
    });
});

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Always on, including IIS/Production — this is an offline-intranet deployment (not
// internet-facing), and the whole point of exposing it is so the API can be smoke-tested against
// a real IIS-hosted instance. Registered before CallerRestrictionMiddleware/UseAuthentication/
// UseAuthorization below, so loading the Swagger UI/JSON itself never needs the shared internal
// key or a bearer token — only actually *invoking* an endpoint via "Try it out" does, same as any
// other caller. Previously gated on IsDevelopment()/IsProduction(), which should already have
// covered a default IIS deployment (ASPNETCORE_ENVIRONMENT defaults to "Production" when unset),
// but removing the check entirely rules out any environment-detection ambiguity.
app.UseSwagger();
app.UseSwaggerUI();

// Offline-intranet default: HTTP only. Set dbo.AppSettings.EnableHttps = 1 for a TLS-terminated
// deployment; never enable this switch for anything internet-facing without also reviewing
// the rest of the security configuration. Read once here (not per-request) since it decides which
// middleware gets registered into the pipeline - a process-lifetime decision, not something
// re-evaluated per request; a flag flip takes effect on the next app restart.
var useHttps = app.Services.GetRequiredService<AppSettingsReader>().GetBoolAtStartup("EnableHttps");
if (useHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CallerRestrictionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
