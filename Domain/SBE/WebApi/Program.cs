using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.Sbe.Application.Interfaces;
using UBIS.Services.Sbe.Infrastructure.Caching;
using UBIS.Services.Sbe.Infrastructure.Configuration;
using UBIS.Services.Sbe.Infrastructure.Persistence;
using UBIS.Services.Sbe.Infrastructure.Security;
using UBIS.Services.Sbe.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// Needed by CallerRestrictionMiddleware and any future audit-stamping helper (caller UserId/
// UserName/client-IP) — requests reach this service through the Gateway, so IP resolution also
// checks X-Forwarded-For, not just the direct connection.
builder.Services.AddHttpContextAccessor();

// DB-first: no EF migrations anywhere in this solution — schema is owned by the DBA / the legacy
// app. The 8 new tables (M_SubCategory, M_SBEDemand, SBENotes, M_DemandCeiling, M_ObjectCeiling,
// PSECategory, PSEDemand, PSECategoryDemand) were created by
// Others/publish-staging/sbe-workstream-1-schema.sql (additive-only, idempotent).
builder.Services.AddDbContext<SbeDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// DB-backed feature flags (DeveloperEnv/EnableCallerRestriction/EnableHttps) — same dbo.AppSettings
// table every other microservice reads; see AppSettingsReader's own doc comment.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

// Redis: session-revocation check below AND the two-tier ICacheService for Category/SubCategory/
// Scheme/SubScheme/UmbrellaScheme/MajorHead lookups and the resolved Demand ceiling.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.")));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Server-side role gate for actions restricted to one of SBE's three FRS roles — see
// RequireSbeRoleFilter's doc comment for why this must be enforced here, not just hidden behind
// UBIS_Web's UI.
builder.Services.Configure<SbeRoleOptions>(builder.Configuration.GetSection("Sbe:Roles"));

builder.Services.Configure<CallerRestrictionOptions>(builder.Configuration.GetSection("CallerRestriction"));

// Validates the JWT AIM issued at login (never issues one here) — MapInboundClaims stays false so
// the "sub"/"RoleId" claims keep their short names exactly as AIM wrote them.
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

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Offline-intranet default: HTTP only. Set dbo.AppSettings.EnableHttps = 1 for a TLS-terminated
// deployment. Read once here (process-lifetime decision), matching every other microservice.
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "SBE" })).AllowAnonymous();

app.Run();
