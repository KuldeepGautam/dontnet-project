using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.Ecl.Application.Interfaces;
using UBIS.Services.Ecl.Infrastructure.Configuration;
using UBIS.Services.Ecl.Infrastructure.Persistence;
using UBIS.Services.Ecl.Infrastructure.Security;
using UBIS.Services.Ecl.Infrastructure.Services;
using UBIS.Services.Ecl.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// Needed by EclOutlayRepository's audit-stamping helper (caller UserId/UserName/client-IP) and
// CallerRestrictionMiddleware — requests reach this service through the Gateway, so IP resolution
// also checks X-Forwarded-For, not just the direct connection.
builder.Services.AddHttpContextAccessor();

// DB-first: no EF migrations anywhere in this solution — schema is owned by the DBA / the legacy
// app that still writes to dbo.ECL_T_Outlay. The two new columns and two new small tables were
// created by Others/publish-staging/ecl-workstream-1-schema.sql (additive-only, idempotent).
builder.Services.AddDbContext<EclDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// DB-backed feature flags (DeveloperEnv/EnableCallerRestriction/EnableHttps) — same dbo.AppSettings
// table every other microservice reads; see AppSettingsReader's own doc comment.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

builder.Services.AddScoped<IEclOutlayRepository, EclOutlayRepository>();

builder.Services.Configure<EclDocumentStorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IEclDocumentStorage, EclDocumentStorageService>();

// Server-side DOE-role gate for EclApprovalController — see RequireDoeRoleFilter's doc comment for
// why this must be enforced here, not just hidden behind UBIS_Web's UI.
builder.Services.Configure<DoeRoleOptions>(builder.Configuration.GetSection("Ecl:DoeRoles"));
builder.Services.AddScoped<RequireDoeRoleFilter>();

builder.Services.Configure<CallerRestrictionOptions>(builder.Configuration.GetSection("CallerRestriction"));

// Redis connection for the OnTokenValidated session-revocation check below (FR-006, added
// 2026-08-21) — this service issues/stores nothing in Redis itself, it only ever reads the
// ubis:session:{sid} key AIM writes, so no ICacheService/two-tier cache is needed here, just a
// raw IConnectionMultiplexer.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.")));

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

// Second line of defense (alongside [RequestSizeLimit]/[RequestFormLimits] on the upload action
// itself) against an oversized PDF upload — the framework rejects it before it ever reaches
// EclDocumentStorageService.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 6 * 1024 * 1024; // 5 MB cap + headroom
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ECL" })).AllowAnonymous();

app.Run();
