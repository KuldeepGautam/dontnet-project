namespace UBIS.Services.Aim.Infrastructure.Configuration;

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads feature-flag rows from dbo.AppSettings (Id/SettingName/SettingValue bit) instead of this
/// service carrying its own copy of these toggles in appsettings.json - see
/// Others/publish-staging/create-appsettings-table.sql. Replaces the old
/// Security:UseHttps/SecurityConfiguration:EnableUserIPSettings config reads (client requirement
/// 2026-08-07: centralize EnableEmail/EnableIPLogging/EnableHttps/EnableCertificate in the DB so
/// flipping dev vs. production posture is one row update instead of editing every service's config
/// file). Cached in-memory for CacheDuration so per-request checks (EnableIPLogging) don't hit the
/// DB on every call; a flag change takes effect within that window without an app restart, EXCEPT
/// EnableHttps/EnableCertificate, which are only ever read once at startup (see Program.cs) since
/// they gate which ASP.NET Core middleware gets registered into the pipeline - a decision fixed for
/// the process lifetime, not re-evaluated per request. This same class is duplicated (not shared)
/// per microservice, matching this solution's existing convention of no cross-service class library
/// (see JwtValidationFactory/CallerRestrictionMiddleware, each independently copied per service).
/// </summary>
public class AppSettingsReader
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private readonly string _connectionString;
    private readonly ILogger<AppSettingsReader> _logger;
    private readonly ConcurrentDictionary<string, (bool Value, DateTime ExpiresAtUtc)> _cache = new();
    private readonly ConcurrentDictionary<string, (int Value, DateTime ExpiresAtUtc)> _intCache = new();

    public AppSettingsReader(string connectionString, ILogger<AppSettingsReader> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Blocking read for use during Program.cs startup, before the host is built (no DI/async
    /// context available yet). Only EnableHttps/EnableCertificate should ever use this - anything
    /// read per-request should use <see cref="GetBoolAsync"/> instead.
    /// </summary>
    public bool GetBoolAtStartup(string settingName, bool defaultValue = false)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = new SqlCommand("SELECT SettingValue FROM dbo.AppSettings WHERE SettingName = @name", connection);
            command.Parameters.AddWithValue("@name", settingName);
            var result = command.ExecuteScalar();
            return result is bool value ? value : defaultValue;
        }
        catch (Exception ex)
        {
            // dbo.AppSettings unreachable/missing at startup - fail safe to the given default
            // (matches this app's existing "a down dependency is logged, never fatal to startup"
            // convention for SQL/Redis/RabbitMQ connectivity checks below) rather than crash.
            _logger.LogWarning(ex, "Could not read AppSettings.{SettingName} at startup; using default {Default}.", settingName, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>Cached async read for per-request use once the app is running.</summary>
    public async Task<bool> GetBoolAsync(string settingName, bool defaultValue = false, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(settingName, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
        {
            return cached.Value;
        }

        bool value;
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var command = new SqlCommand("SELECT SettingValue FROM dbo.AppSettings WHERE SettingName = @name", connection);
            command.Parameters.AddWithValue("@name", settingName);
            var result = await command.ExecuteScalarAsync(ct);
            value = result is bool b ? b : defaultValue;
        }
        catch (Exception ex)
        {
            // Fall back to the last known-good cached value if there is one, else the default -
            // a transient DB hiccup shouldn't flip a security-relevant flag's effective value.
            value = _cache.TryGetValue(settingName, out var stale) ? stale.Value : defaultValue;
            _logger.LogWarning(ex, "Could not read AppSettings.{SettingName}; using {Value}.", settingName, value);
        }

        _cache[settingName] = (value, DateTime.UtcNow.Add(CacheDuration));
        return value;
    }

    /// <summary>Reads all 5 well-known flags in one round trip - used by the api/app-settings endpoint UBIS_Web polls (it has no direct DB access of its own).</summary>
    public async Task<AppSettingsSnapshot> GetAllAsync(CancellationToken ct = default)
    {
        return new AppSettingsSnapshot(
            EnableEmail: await GetBoolAsync("EnableEmail", ct: ct),
            EnableIPLogging: await GetBoolAsync("EnableIPLogging", ct: ct),
            EnableHttps: await GetBoolAsync("EnableHttps", ct: ct),
            EnableCertificate: await GetBoolAsync("EnableCertificate", ct: ct),
            EnableSms: await GetBoolAsync("EnableSms", ct: ct),
            IdleTimerMinutes: await GetIntAsync("IdleTimer", defaultValue: 15, ct: ct));
    }

    /// <summary>
    /// Integer-valued sibling of <see cref="GetBoolAsync"/>, reading dbo.AppSettingsInt (Id/
    /// SettingName/SettingValue int) instead of dbo.AppSettings (bit-only) - see
    /// Others/publish-staging/appsettings-int-idle-timer.sql. Client requirement 2026-09-04:
    /// "make entry in settings table for IdleTimer and value 15. What ever value of this field,
    /// the timer should start from same value" - this is now the single source of truth for the
    /// idle-logout countdown length (UBIS_Web's SessionCountdownViewComponent), replacing the old
    /// admin-set Redis-cache override (AdminController.SessionSettings) that had silently drifted
    /// to a stale value with no visible record of where it came from.
    /// </summary>
    public async Task<int> GetIntAsync(string settingName, int defaultValue = 0, CancellationToken ct = default)
    {
        if (_intCache.TryGetValue(settingName, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
        {
            return cached.Value;
        }

        int value;
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var command = new SqlCommand("SELECT SettingValue FROM dbo.AppSettingsInt WHERE SettingName = @name", connection);
            command.Parameters.AddWithValue("@name", settingName);
            var result = await command.ExecuteScalarAsync(ct);
            value = result is int i ? i : defaultValue;
        }
        catch (Exception ex)
        {
            value = _intCache.TryGetValue(settingName, out var stale) ? stale.Value : defaultValue;
            _logger.LogWarning(ex, "Could not read AppSettingsInt.{SettingName}; using {Value}.", settingName, value);
        }

        _intCache[settingName] = (value, DateTime.UtcNow.Add(CacheDuration));
        return value;
    }

    /// <summary>Admin-triggered update (AppSettingsController.UpdateIdleTimerMinutes) - upserts the row and invalidates this instance's own cached copy so the next read reflects it immediately, without waiting out CacheDuration.</summary>
    public async Task SetIntAsync(string settingName, int value, CancellationToken ct = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = new SqlCommand(
            "UPDATE dbo.AppSettingsInt SET SettingValue = @value WHERE SettingName = @name; " +
            "IF @@ROWCOUNT = 0 INSERT INTO dbo.AppSettingsInt (SettingName, SettingValue) VALUES (@name, @value);",
            connection);
        command.Parameters.AddWithValue("@name", settingName);
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(ct);

        _intCache[settingName] = (value, DateTime.UtcNow.Add(CacheDuration));
    }
}

public record AppSettingsSnapshot(bool EnableEmail, bool EnableIPLogging, bool EnableHttps, bool EnableCertificate, bool EnableSms, int IdleTimerMinutes);
