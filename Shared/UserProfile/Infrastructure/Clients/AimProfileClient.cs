namespace UBIS.Services.UserProfile.Infrastructure.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UBIS.Services.UserProfile.Application.Interfaces;

/// <summary>
/// Calls AIM's existing profile endpoints server-to-server, forwarding the caller's own bearer
/// token — AIM stays the sole owner of dbo.M_User, UserProfile never reads it directly.
/// </summary>
public class AimProfileClient : IAimProfileClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AimProfileClient> _logger;

    public AimProfileClient(HttpClient http, ILogger<AimProfileClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<AimProfileSnapshot?> GetProfileSnapshotAsync(string bearerToken, CancellationToken ct = default)
    {
        var profile = await GetAsync<LegacyProfileResponse>("api/users/legacy-profile", bearerToken, ct);
        var compliance = await GetAsync<ComplianceStatusResponse>("api/users/compliance-status", bearerToken, ct);

        if (profile == null)
        {
            return null;
        }

        return new AimProfileSnapshot(
            profile.Username,
            profile.Role,
            profile.LastLoginDate,
            compliance?.AllowedIpAddressOne,
            compliance?.AllowedIpAddressTwo,
            profile.Email,
            profile.MaskedMobile);
    }

    public async Task<bool> UpdateContactAsync(string bearerToken, string? email, string? mobile, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/users/contact");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            request.Content = JsonContent.Create(new { Email = email, Mobile = mobile });

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AIM contact-update call returned {StatusCode}.", (int)response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AIM contact-update call failed.");
            return false;
        }
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, string bearerToken, CancellationToken ct) where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AIM call to {Url} returned {StatusCode}.", relativeUrl, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AIM call to {Url} failed.", relativeUrl);
            return null;
        }
    }

    /// <summary>Local copy of AIM's LegacyProfileDto — only the fields UserProfile needs.
    /// MaskedMobile renamed from plaintext Mobile 2026-08-17 (AIM Workstream 7).</summary>
    private sealed class LegacyProfileResponse
    {
        public string? Username { get; set; }
        public string? Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? Email { get; set; }
        public string? MaskedMobile { get; set; }
    }

    /// <summary>Local copy of AIM's ComplianceStatusDto — only the fields UserProfile needs.</summary>
    private sealed class ComplianceStatusResponse
    {
        public string? AllowedIpAddressOne { get; set; }
        public string? AllowedIpAddressTwo { get; set; }
    }
}
