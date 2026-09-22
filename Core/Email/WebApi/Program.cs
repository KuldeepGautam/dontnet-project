using Microsoft.EntityFrameworkCore;
using Serilog;
using UBIS.Services.Email.Application.Interfaces;
using UBIS.Services.Email.Infrastructure.Configuration;
using UBIS.Services.Email.Infrastructure.Logging;
using UBIS.Services.Email.Infrastructure.Messaging;
using UBIS.Services.Email.Infrastructure.Persistence;
using UBIS.Services.Email.Infrastructure.Smtp;

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
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// ============================================================================
// Persistence — M_EmailDispatchLog (SQL Server, dbo schema). DB-first: no EF migrations
// anywhere in this solution — schema is owned by Core/Email/SQL/*.sql, DBA-reviewed.
// ============================================================================
builder.Services.AddDbContext<EmailDbContext>(options =>
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

// ============================================================================
// Messaging + email sending
// ============================================================================
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<ILogWriterClient, RabbitMqLogWriterClient>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<RabbitMqOtpEmailConsumer>();
builder.Services.AddHostedService<RabbitMqNotificationEmailConsumer>();

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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "EmailService" }));

// ============================================================================
// Database Initialization
// ============================================================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Log.Information("EmailService database connection successful.");
        }
        else
        {
            Log.Warning("Could not connect to the EmailService database. Check the connection string.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during EmailService database initialization.");
    }
}

app.Run();
