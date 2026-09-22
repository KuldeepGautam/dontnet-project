namespace UBIS.Web.Services.Session;

using System.Text.Json;
using Microsoft.AspNetCore.Http;

public static class SessionExtensions
{
    private const string UbisSessionKey = "UbisSessionData";

    public static void SetUbisSession(this ISession session, UbisSessionData data) =>
        session.SetString(UbisSessionKey, JsonSerializer.Serialize(data));

    public static UbisSessionData? GetUbisSession(this ISession session)
    {
        var raw = session.GetString(UbisSessionKey);
        return raw == null ? null : JsonSerializer.Deserialize<UbisSessionData>(raw);
    }

    public static void ClearUbisSession(this ISession session) => session.Remove(UbisSessionKey);
}
