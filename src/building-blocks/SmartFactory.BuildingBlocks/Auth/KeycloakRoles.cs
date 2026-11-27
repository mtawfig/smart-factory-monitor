using System.Security.Claims;
using System.Text.Json;

namespace SmartFactory.BuildingBlocks.Auth;

/// <summary>
/// Keycloak encodes realm roles as <c>{"realm_access":{"roles":["technician", ...]}}</c>.
/// ASP.NET Core expects one role claim per role. This lifts them.
/// </summary>
public static class KeycloakRoles
{
    private const string RealmAccessClaim = "realm_access";

    public static void LiftRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = identity.FindFirst(RealmAccessClaim)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        foreach (var role in ParseRoles(realmAccess))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    /// <summary>Parses the JSON value of a <c>realm_access</c> claim. Malformed input yields no roles.</summary>
    public static IReadOnlyList<string> ParseRoles(string realmAccessJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(realmAccessJson);
            if (!doc.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return [.. roles.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString()!)
                .Where(r => !string.IsNullOrWhiteSpace(r))];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
