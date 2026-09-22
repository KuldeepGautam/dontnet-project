using System.Runtime.Versioning;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UBIS.Services.LogWriter.Configuration;
using UBIS.Services.LogWriter.Persistence;
using UBIS.Services.LogWriter.Services;

// This service only ever runs under IIS on Windows (writes to the Windows Event Log below, among
// other Windows-only hosting assumptions elsewhere in this solution) - declaring that here is what
// actually resolves CA1416 on the EventLog sink call, rather than suppressing the warning blindly.
[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();

// Serilog setup
// EventLog sink writes into Windows Logs > Application under the "ubis2" source (2026-07-21).
// manageEventSource: false deliberately - creating an Event Source requires local admin rights,
// which the IIS app pool identity doesn't have; the source must be pre-registered once via an
// elevated script (see publish-staging/Register-EventLogSource.ps1) before this sink can write.
// If the source isn't registered yet, WriteEntry throws - caught by Serilog's own internal
// SelfLog-guarded try/catch around each sink, so a missing source degrades to "EventLog entries
// silently don't appear" rather than crashing the app, same failure shape as the File sink's
// earlier IIS permission gap.
var serilogConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/logwriter.log", rollingInterval: RollingInterval.Day)
    .WriteTo.EventLog("ubis2", manageEventSource: false);
Log.Logger = serilogConfig.CreateLogger();

builder.Host.UseSerilog();

// DbContext
// DB-first: no EF migrations anywhere in this solution — schema is owned by
// Core/LogWriter/SQL/*.sql, DBA-reviewed.
builder.Services.AddDbContext<LogDbContext>(options =>
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RabbitMQ listener
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddHostedService<RabbitMqListenerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

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

app.MapControllers();

app.Run();
