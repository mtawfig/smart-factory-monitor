using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests;

/// <summary>
/// Boots the real host in-process without a database or Keycloak and checks the HTTP pipeline contract that the
/// Angular shell, the Compose health checks and the content rules rely on.
/// </summary>
public sealed class HostSmokeTests(NoDatabaseFactory factory) : IClassFixture<NoDatabaseFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Liveness_endpoint_is_healthy_without_any_dependency()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/health/live", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Every_response_carries_the_synthetic_data_header()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/health/live", UriKind.Relative), Ct);

        response.Headers.TryGetValues(SyntheticDataHeaderMiddleware.HeaderName, out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Contain("synthetic").And.Contain("advisory-only");
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().Contain(h => h.Scheme == "Bearer");
    }

    [Fact]
    public void Every_api_endpoint_requires_authorization()
    {
        // Fitness function: an endpoint under /api without an authorization requirement fails the build.
        var open = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .Where(e => e.Metadata.GetMetadata<IAuthorizeData>() is null)
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        open.Should().BeEmpty("every /api endpoint must require an authenticated, tenant-scoped caller (ADR-008)");
    }

    [Theory]
    [InlineData("/api/assets/assets")]
    [InlineData("/api/assets/plants")]
    [InlineData("/api/audit/events")]
    [InlineData("/api/maintenance/work-orders")]
    [InlineData("/api/inventory/ping")]
    public async Task Anonymous_calls_are_rejected(string path)
    {
        var response = await factory.CreateClient().GetAsync(new Uri(path, UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
