using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.ReferenceData.Application.Interfaces;
using UBIS.Services.ReferenceData.Infrastructure.Configuration;
using UBIS.Services.ReferenceData.Infrastructure.Persistence;
using UBIS.Services.ReferenceData.Infrastructure.Security;
using UBIS.Services.ReferenceData.Infrastructure.Services;
using UBIS.Services.ReferenceData.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// DB-first: no EF migrations anywhere in this solution — schema is owned by the DBA;
// dbo.M_Scheme/M_SubScheme/M_MajorHead/M_ObjectHead are created by
// SQL Queries\new-tables\ReferenceData_CreateTables.sql, not by this service.
builder.Services.AddDbContext<ReferenceDataDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();

// DB-backed feature flags (EnableEmail/EnableIPLogging/EnableHttps/EnableCertificate) - see
// dbo.AppSettings and AppSettingsReader's own doc comment for why this replaced the old
// Security:UseHttps/SecurityConfiguration:EnableUserIPSettings config reads.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

builder.Services.Configure<CallerRestrictionOptions>(builder.Configuration.GetSection("CallerRestriction"));

// Redis connection for the OnTokenValidated session-revocation check below (FR-006, added
// 2026-08-21) — this service issues/stores nothing in Redis itself, it only ever reads the
// ubis:session:{sid} key AIM writes, so no ICacheService/two-tier cache is needed here, just a
// raw IConnectionMultiplexer.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.")));

// Validates the JWT AIM issued at login (never issues one here) — MapInboundClaims stays false
// so the "sub" claim keeps its short name exactly as AIM wrote it.
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
