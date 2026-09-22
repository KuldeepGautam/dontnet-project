using Microsoft.EntityFrameworkCore;
using Serilog;
using UBIS.Services.MobilePhone.Application.Interfaces;
using UBIS.Services.MobilePhone.Infrastructure.Configuration;
using UBIS.Services.MobilePhone.Infrastructure.Logging;
using UBIS.Services.MobilePhone.Infrastructure.Messaging;
using UBIS.Services.MobilePhone.Infrastructure.Persistence;
using UBIS.Services.MobilePhone.Infrastructure.Sms;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Serilog — local console/file sinks. Centralized log/warn/error events are
// additionally shipped to the LogWriter microservice via RabbitMqLogWriterClient.
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// ============================================================================
// Configuration
// ============================================================================
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// ============================================================================
// Persistence — M_SmsDispatchLog (SQL Server, dbo schema). DB-first: no EF migrations
// anywhere in this solution — schema is owned by Core/MobilePhone/SQL/*.sql, DBA-reviewed.
// ============================================================================
builder.Services.AddDbContext<MobilePhoneDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// DB-backed feature flag (EnableSms) - see dbo.AppSettings and AppSettingsReader's own doc
// comment for why this replaced per-service config-file toggles.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

// ============================================================================
// Messaging + SMS sending
// ============================================================================
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<ILogWriterClient, RabbitMqLogWriterClient>();
builder.Services.AddScoped<ISmsSender, NotConfiguredSmsSender>();
builder.Services.AddHostedService<RabbitMqNotificationSmsConsumer>();

var app = builder.Build();

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

// Internal-only liveness probe for IIS/ops; this service has no public HTTP surface.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MobilePhoneService" }));

// ============================================================================
// Database Initialization
// ============================================================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MobilePhoneDbContext>();
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Log.Information("MobilePhoneService database connection successful.");
        }
        else
        {
            Log.Warning("Could not connect to the MobilePhoneService database. Check the connection string.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during MobilePhoneService database initialization.");
    }
}

app.Run();
