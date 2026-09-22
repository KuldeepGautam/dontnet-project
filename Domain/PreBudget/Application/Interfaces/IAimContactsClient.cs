namespace UBIS.Services.PreBudget.Application.Interfaces;

using UBIS.Services.PreBudget.Application.DTOs;

/// <summary>Server-to-server call to AIM's GET api/users/contacts-by-role - PreBudget never owns copies of AIM's user/role data. Added 2026-08-14 for the Allocation screen's notification feature.</summary>
public interface IAimContactsClient
{
    Task<IReadOnlyList<RecipientContactDto>> GetContactsByRoleAsync(
        IReadOnlyList<string> roleNames, int? demandId, string bearerToken, CancellationToken ct = default);
}
