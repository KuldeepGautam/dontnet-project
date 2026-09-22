namespace UBIS.Web.Services.Clients;

public interface IMenuClient
{
    /// <summary>
    /// Fetches the menu tree for whatever role the given AIM-issued JWT's session resolves to —
    /// MenuGenerator derives the role itself (via the token's "sid" claim against Redis), so no
    /// roleName is sent on the wire.
    /// </summary>
    Task<ApiCallResult<MenuFullResponseDto>> GetFullMenuAsync(string bearerToken, CancellationToken ct = default);
}
