using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;
using UBIS.Services.Reporting.Infrastructure.Configuration;
using UBIS.Services.Reporting.Infrastructure.Security;
using UBIS.Services.Reporting.WebApi.Security;
using UBIS.Services.Reporting.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// Needed by CallerRestrictionMiddleware — requests reach this service through the Gateway, so IP
// resolution also checks X-Forwarded-For, not just the direct connection.
builder.Services.AddHttpContextAccessor();

// This service is stateless (no domain data of its own to persist — every export request carries
// its own report data) so it deliberately has no DbContext/EF Core, unlike every other microservice
// in this solution. The one thing it still reads from SQL is dbo.AppSettings, via the same
// lightweight ADO.NET AppSettingsReader every other microservice uses for its feature flags.
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

// PdfSharp 6.x has no bundled fonts or default GDI lookup — see WindowsFontResolver's own doc
// comment. GlobalFontSettings.FontResolver is process-wide, static state, so this is set once here
// rather than per-request.
PdfSharp.Fonts.GlobalFontSettings.FontResolver = new WindowsFontResolver();

builder.Services.AddScoped<PdfReportGenerator>();
builder.Services.AddScoped<ExcelReportGenerator>();
builder.Services.AddScoped<CsvReportGenerator>();

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

// A report request's JSON body (rows for a large grouped report) can be sizeable — matches ECL's
// own request-size headroom rather than the ASP.NET Core default.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 20 * 1024 * 1024; // 20 MB
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Reporting" })).AllowAnonymous();

app.Run();
