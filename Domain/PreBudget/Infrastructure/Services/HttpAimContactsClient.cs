namespace UBIS.Services.PreBudget.Infrastructure.Services;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;

/// <summary>
/// Calls AIM's contacts-by-role endpoint using the named "Aim" HttpClient (registered in
/// Program.cs, matching the existing "ReferenceData" client's shape). Forwards the caller's own
/// bearer token rather than minting a separate service credential - AIM issues every token this
/// solution uses and PreBudget already only ever validates (never issues) them, so the caller's
/// token is already valid for AIM too (same secret/issuer/audience, see appsettings.json's
/// SecurityConfiguration:Jwt comment). Added 2026-08-14.
/// </summary>
public class HttpAimContactsClient : IAimContactsClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpAimContactsClient> _logger;

    public HttpAimContactsClient(IHttpClientFactory httpClientFactory, ILogger<HttpAimContactsClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecipientContactDto>> GetContactsByRoleAsync(
        IReadOnlyList<string> roleNames, int? demandId, string bearerToken, CancellationToken ct = default)
    {
        if (roleNames.Count == 0)
        {
            return Array.Empty<RecipientContactDto>();
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Aim");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var query = $"api/users/contacts-by-role?roleNames={Uri.EscapeDataString(string.Join(",", roleNames))}";
            if (demandId.HasValue)
            {
                query += $"&demandId={demandId.Value}";
            }

            var response = await client.GetFromJsonAsync<ContactsByRoleResponse>(query, ct);
            return response?.Contacts ?? new List<RecipientContactDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve recipient contacts from AIM for roles {RoleNames}; those recipients will not be notified.", string.Join(",", roleNames));
            return Array.Empty<RecipientContactDto>();
        }
    }

    private class ContactsByRoleResponse
    {
        public List<RecipientContactDto> Contacts { get; set; } = new();
    }
}
