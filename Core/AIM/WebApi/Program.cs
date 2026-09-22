using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Caching;
using UBIS.Services.Aim.Infrastructure.Configuration;
using UBIS.Services.Aim.Infrastructure.Logging;
using UBIS.Services.Aim.Infrastructure.Messaging;
using UBIS.Services.Aim.Infrastructure.Otp;
using UBIS.Services.Aim.Infrastructure.PasswordPolicy;
using UBIS.Services.Aim.Infrastructure.Persistence;
using UBIS.Services.Aim.Infrastructure.Security;
using UBIS.Services.Aim.Infrastructure.Services;
using UBIS.Services.Aim.Infrastructure.Sms;
using UBIS.Services.Aim.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("SecurityConfiguration:Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration section 'SecurityConfiguration:Jwt' is missing.");

// ============================================================================
// Add Services to the Dependency Injection Container
// ============================================================================

// Configure Entity Framework Core with SQL Server. DB-first as of 2026-07-13: no EF migrations
// anywhere in this solution, no PostgreSQL support (was config placeholders only, never wired
// to real code) — this is the single BIMS2 database shared by every project.
builder.Services.AddDbContext<AimDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
);

// Add HTTP context accessor for IP extraction
builder.Services.AddHttpContextAccessor();

// DB-backed feature flags (EnableEmail/EnableIPLogging/EnableHttps/EnableCertificate) - see
// dbo.AppSettings and AppSettingsReader's own doc comment for why this replaced the old
// Security:UseHttps/SecurityConfiguration:EnableUserIPSettings config reads.
builder.Services.AddSingleton(sp => new AppSettingsReader(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    sp.GetRequiredService<ILogger<AppSettingsReader>>()));

// Register Application Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IStatementAccessService, StatementAccessService>();
builder.Services.AddScoped<IComplianceNotificationService, ComplianceNotificationService>();

// Redis-backed cache (sessions, refresh tokens, rate limits, reset tokens)
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConfig = builder.Configuration.GetSection("CacheConfiguration:Redis");
    var options = new ConfigurationOptions
    {
        EndPoints = { $"{redisConfig["Host"]}:{redisConfig["Port"]}" },
        Password = string.IsNullOrEmpty(redisConfig["Password"]) ? null : redisConfig["Password"],
        Ssl = redisConfig.GetValue<bool>("Ssl"),
        ConnectTimeout = redisConfig.GetValue<int?>("ConnectTimeout") ?? 5000,
        SyncTimeout = redisConfig.GetValue<int?>("SyncTimeout") ?? 5000,
        DefaultDatabase = redisConfig.GetValue<int?>("Database") ?? 0,
        AbortOnConnectFail = false
    };
    return ConnectionMultiplexer.Connect(options);
});
// L1 (in-process) tier for the two-tier ICacheService above.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Mobile-number encryption (Workstream 7, added 2026-08-17) — persistent key ring (not the
// framework default in-memory one) so encrypted M_User.EncryptedMobile values stay decryptable
// across app restarts/multiple instances; mirrors UBIS_Web's own AddDataProtection()
// .PersistKeysToFileSystem setup (Program.cs there) with a sibling folder next to WebApi's own
// content root rather than sharing UBIS_Web's key ring — these are two different applications'
// protected data and must not share a key ring/purpose namespace.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "..", "AIM-DataProtection-Keys")))
    .SetApplicationName("AIM.WebApi");
builder.Services.AddScoped<IMobileProtectionService, MobileProtectionService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("SecurityConfiguration:Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep short claim names ("sub", "RoleName", ...) as issued
        options.TokenValidationParameters = JwtTokenFactory.CreateValidationParameters(jwtOptions);
        options.Events = JwtSessionRevocationEvents.Create();
    });
builder.Services.AddAuthorization();

// New services from addenda
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();
builder.Services.AddScoped<UBIS.Services.Aim.Application.Interfaces.IPasswordHistoryService, UBIS.Services.Aim.Infrastructure.Services.PasswordHistoryService>();

// Login MFA (added 2026-07) — default off; SMS channel is a stub until a real gateway exists.
builder.Services.AddSingleton<ISmsServiceClient, NotConfiguredSmsServiceClient>();

// Bind options
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection("Otp"));
builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));
builder.Services.Configure<MfaOptions>(builder.Configuration.GetSection("Mfa"));
builder.Services.Configure<PasswordResetOtpOptions>(builder.Configuration.GetSection("PasswordResetOtp"));
builder.Services.Configure<CallerRestrictionOptions>(builder.Configuration.GetSection("CallerRestriction"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<AdminRoleOptions>(builder.Configuration.GetSection("SecurityConfiguration:AdminRoles"));

// Messaging clients: EmailService (OTP dispatch) + LogWriter (centralized logging)
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<ILogWriterClient, RabbitMqLogWriterClient>();
builder.Services.AddSingleton<IEmailServiceClient, RabbitMqEmailServiceClient>();

// Add CORS if needed for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add Controllers
builder.Services.AddControllers();

// Add API documentation/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
});

// ============================================================================
// Build the Application
// ============================================================================

var app = builder.Build();

// Configure the HTTP request pipeline
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

app.UseCors("AllowAll");

// Caller restriction middleware should run before authentication/authorization. The middleware
// itself no-ops entirely while dbo.AppSettings.DeveloperEnv is on, so it's safe to always register.
app.UseMiddleware<CallerRestrictionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================================================
// Infrastructure Connectivity Checks (SQL Server, Redis, RabbitMQ)
// DB-first: no migrations are ever applied here, this only verifies connectivity and logs
// the result — a down dependency is logged, never fatal to startup (matches how every
// RabbitMQ/Redis client elsewhere in this solution already tolerates a down broker at
// call-time).
// ============================================================================
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AimDbContext>();
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            startupLogger.LogInformation("SQL Server connection successful.");
        }
        else
        {
            startupLogger.LogWarning("Could not connect to SQL Server. Check the connection string.");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Error verifying SQL Server connection.");
    }

    try
    {
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        if (redis.IsConnected)
        {
            await redis.GetDatabase().PingAsync();
            startupLogger.LogInformation("Redis connection successful.");
        }
        else
        {
            startupLogger.LogWarning("Could not connect to Redis. Check the Redis configuration.");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Error verifying Redis connection.");
    }

    try
    {
        var rabbitMq = scope.ServiceProvider.GetRequiredService<RabbitMqConnectionProvider>();
        await using var channel = await rabbitMq.CreateChannelAsync();
        startupLogger.LogInformation("RabbitMQ connection successful.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Error verifying RabbitMQ connection.");
    }
}

app.Run();
