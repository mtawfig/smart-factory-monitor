using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.Platform.Api.Tests.Infrastructure;

/// <summary>
/// Replaces Keycloak in tests: the caller states who they are in request headers. Everything downstream of
/// authentication (policies, tenant resolution, EF filters, row-level security) runs exactly as in production.
/// </summary>
internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubjectHeader = "X-Test-Sub";
    public const string TenantHeader = "X-Test-Tenant";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeader, out var subject) || string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new("sub", subject!), new("preferred_username", subject!) };
        if (Request.Headers.TryGetValue(TenantHeader, out var tenant) && !string.IsNullOrWhiteSpace(tenant))
        {
            claims.Add(new Claim(TenantClaims.TenantId, tenant!));
        }

        foreach (var role in Request.Headers[RolesHeader].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }
}

internal static class TestUsers
{
    public const string TenantA = "tnt-alsafa";
    public const string TenantB = "tnt-beta";

    public static HttpClient As(this HttpClient client, string tenant, params string[] roles) =>
        client.AsUser(tenant, $"{string.Join('-', roles)}@{tenant}", roles);

    /// <summary>Signs in as a named user; needed where the username matters (work-order assignment).</summary>
    public static HttpClient AsUser(this HttpClient client, string tenant, string username, params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.TenantHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, username);
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, tenant);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        client.DefaultRequestHeaders.Accept.ParseAdd(MediaTypeHeaderValue.Parse("application/json").ToString());
        return client;
    }
}
