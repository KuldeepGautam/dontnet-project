namespace UBIS.Services.Sbe.Infrastructure.Configuration;

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads feature-flag rows from dbo.AppSettings (Id/SettingName/SettingValue bit) — same shared
/// table every other microservice in this solution reads. This class is independently copied per
/// microservice (not a shared library), matching this repo's existing convention (see ECL's own
/// AppSettingsReader/JwtValidationFactory/CallerRestrictionMiddleware, each duplicated rather than
/// shared).
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

    /// <summary>Blocking read for use during Program.cs startup, before the host is built.</summary>
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
            value = _cache.TryGetValue(settingName, out var stale) ? stale.Value : defaultValue;
            _logger.LogWarning(ex, "Could not read AppSettings.{SettingName}; using {Value}.", settingName, value);
        }

        _cache[settingName] = (value, DateTime.UtcNow.Add(CacheDuration));
        return value;
    }
}
