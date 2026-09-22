namespace UBIS.ApiGateway;

using System.Text.Json;

/// <summary>
/// Reads MicroserviceSettings.json (repo root — see UBIS_Web/PAGE_SCAFFOLDING_PLAN.claude §4) and
/// turns it into the in-memory config overlay that overrides ocelot.json's DownstreamHostAndPorts
/// per route, correlated by each route's "ServiceName" field. This is the one file every developer
/// machine edits when a service's address changes — ocelot.json's routing structure (upstream
/// templates, auth, rate limits) stays untouched.
/// </summary>
public static class MicroserviceSettingsLoader
{
    /// <summary>
    /// Builds the override dictionary. Must be called with the already-loaded ocelot.json Routes
    /// array (as a JsonElement) so each override key can be positioned at the correct array index
    /// — Ocelot's config binds from flat "Routes:{index}:DownstreamHostAndPorts:0:Host" keys.
    /// </summary>
    public static Dictionary<string, string?> BuildOverrides(string microserviceSettingsPath, string ocelotJsonPath)
    {
        var overrides = new Dictionary<string, string?>();

        if (!File.Exists(microserviceSettingsPath) || !File.Exists(ocelotJsonPath))
        {
            return overrides;
        }

        var settingsDoc = JsonDocument.Parse(File.ReadAllText(microserviceSettingsPath));
        var byName = new Dictionary<string, (string Host, int Port)>(StringComparer.OrdinalIgnoreCase);
        foreach (var svc in settingsDoc.RootElement.GetProperty("Microservices").EnumerateArray())
        {
            var name = svc.GetProperty("Name").GetString();
            var host = svc.GetProperty("IpAddress").GetString();
            var port = svc.GetProperty("Port").GetInt32();
            if (name != null && host != null)
            {
                byName[name] = (host, port);
            }
        }

        var ocelotDoc = JsonDocument.Parse(File.ReadAllText(ocelotJsonPath));
        var routes = ocelotDoc.RootElement.GetProperty("Routes");
        for (var i = 0; i < routes.GetArrayLength(); i++)
        {
            var route = routes[i];
            // "_XServiceName", not "ServiceName" — the latter is a reserved Ocelot schema key that
            // triggers its service-discovery code path (Consul/Eureka), which broke startup the
            // first time this was tried (confirmed: FileValidationFailedError referencing
            // ServiceDiscoveryFinderDelegate). This is our own correlation field, deliberately named
            // to avoid colliding with Ocelot's actual config surface.
            if (!route.TryGetProperty("_XServiceName", out var serviceNameEl))
            {
                continue;
            }

            var serviceName = serviceNameEl.GetString();
            if (serviceName == null || !byName.TryGetValue(serviceName, out var location))
            {
                continue;
            }

            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Host"] = location.Host;
            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Port"] = location.Port.ToString();
        }

        return overrides;
    }
}
