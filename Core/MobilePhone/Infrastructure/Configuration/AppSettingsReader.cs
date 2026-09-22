namespace UBIS.Services.MobilePhone.Infrastructure.Configuration;

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads feature-flag rows from dbo.AppSettings (Id/SettingName/SettingValue bit) instead of this
/// service carrying its own copy of these toggles in appsettings.json - see
/// Others/publish-staging/create-appsettings-table.sql. Copied verbatim (namespace only change)
/// from Email.Infrastructure.Configuration.AppSettingsReader per this solution's "duplicated not
/// shared" convention. Cached in-memory for CacheDuration so per-request checks (EnableSms) don't
/// hit the DB on every call; a flag change takes effect within that window without an app restart,
/// EXCEPT EnableHttps/EnableCertificate, which are only ever read once at startup (see Program.cs)
/// since they gate which ASP.NET Core middleware gets registered into the pipeline - a decision
/// fixed for the process lifetime, not re-evaluated per request.
/// </summary>
public class AppSettingsReader
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private readonly string _connectionString;
    private readonly ILogger<AppSettingsReader> _logger;
    private readonly ConcurrentDictionary<string, (bool Value, DateTime ExpiresAtUtc)> _cache = new();

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

    /// <summary>Reads the well-known flags this service cares about in one round trip.</summary>
    public async Task<AppSettingsSnapshot> GetAllAsync(CancellationToken ct = default)
    {
        return new AppSettingsSnapshot(
            EnableSms: await GetBoolAsync("EnableSms", ct: ct),
            EnableHttps: await GetBoolAsync("EnableHttps", ct: ct));
    }
}

public record AppSettingsSnapshot(bool EnableSms, bool EnableHttps);
