using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.PreBudget.Application.Interfaces;
using UBIS.Services.PreBudget.Infrastructure.Configuration;
using UBIS.Services.PreBudget.Infrastructure.Messaging;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Security;
using UBIS.Services.PreBudget.Infrastructure.Services;
using UBIS.Services.PreBudget.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// Needed by AppendixDataRepository<T> and PreBudgetDbContext's audit-log override (both capture
// the caller's UserId/UserName/client-IP for the Edit/Modify/Delete + audit trail retrofit,
// 2026-08-11) - requests reach this service through the Gateway, so IP resolution also checks
// X-Forwarded-For, not just the direct connection.
builder.Services.AddHttpContextAccessor();

// DB-first: no EF migrations anywhere in this solution — schema is owned by the DBA;
// every table here is created by SQL Queries\new-tables\PreBudget_CreateTables_01_Master.sql and
// _02_Appendices.sql, not by this service.
builder.Services.AddDbContext<PreBudgetDbContext>(options =>
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

builder.Services.AddScoped(typeof(AppendixDataRepository<>));
builder.Services.AddScoped<IAppendixService, AppendixService>();
builder.Services.AddScoped<IAutonomousBodyService, AutonomousBodyService>();
builder.Services.AddScoped<IRemarkService, RemarkService>();
builder.Services.AddScoped<LegacyAppendixDataMigrationService>();
builder.Services.AddScoped<AppendixPermissionService>();

builder.Services.Configure<RemarkRoleOptions>(builder.Configuration.GetSection("PreBudget:RemarkRoles"));
builder.Services.Configure<AutonomousBodyAdminRoleOptions>(builder.Configuration.GetSection("PreBudget:AutonomousBodyAdminRoles"));
builder.Services.Configure<AutonomousBodyCreatorRoleOptions>(builder.Configuration.GetSection("PreBudget:AutonomousBodyCreatorRoles"));

// Server-to-server call to ReferenceData for Scheme/SubScheme/MajorHead/ObjectHead validation
// (decision 5, design doc §8) — PreBudget never owns copies of that master data.
builder.Services.AddHttpClient("ReferenceData", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MicroserviceUrls:ReferenceData"]
        ?? throw new InvalidOperationException("Configuration value 'MicroserviceUrls:ReferenceData' is missing."));
});

// Server-to-server call to AIM for the Allocation screen's recipient resolution (Budget
// Officer/CCA/FA per Demand, Budget Division Officer/Section User org-wide) - PreBudget never owns
// copies of AIM's user/role data. Forwards the caller's own bearer token (see HttpAimContactsClient).
builder.Services.AddHttpClient("Aim", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MicroserviceUrls:Aim"]
        ?? throw new InvalidOperationException("Configuration value 'MicroserviceUrls:Aim' is missing."));
});
builder.Services.AddScoped<IAimContactsClient, HttpAimContactsClient>();

// Publish-only RabbitMQ setup for Allocation-screen notifications - publishes onto queues
// Core/Email and Core/MobilePhone already own/consume; PreBudget never consumes anything itself.
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddScoped<INotificationPublisher, RabbitMqNotificationPublisher>();

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
// so the "sub"/"RoleName" claims keep their short names exactly as AIM wrote them.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtValidationFactory.CreateValidationParameters(jwtOptions);
        options.Events = JwtSessionRevocationEvents.Create();
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Reshapes [ApiController]'s automatic 400 (property-level DataAnnotations like
// [NotFutureDate]/[NonNegativeAmounts], and any [Required]/[Range] etc.) into the same
// {Code, Message} envelope every controller action already returns for its own explicit
// validation failures. Without this, the default ValidationProblemDetails body doesn't map onto
// AimErrorDto client-side (its Message stays "" instead of null, so the client's `?? "Could not
// save the record."` fallback never kicks in) and the user sees a blank status message instead of
// e.g. "Date of Last Release cannot be a future date." (client report, Appendix III, 2026-08-27).
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)));

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            Code = "VALIDATION_ERROR",
            Message = string.IsNullOrWhiteSpace(message) ? "One or more fields are invalid." : message
        });
    };
});

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
