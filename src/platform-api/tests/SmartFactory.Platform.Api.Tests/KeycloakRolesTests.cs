using System.Security.Claims;
using SmartFactory.BuildingBlocks.Auth;

namespace SmartFactory.Platform.Api.Tests;

public sealed class KeycloakRolesTests
{
    [Fact]
    public void Parses_roles_from_realm_access_json()
    {
        var roles = KeycloakRoles.ParseRoles("""{"roles":["technician","maintenance-planner","offline_access"]}""");

        roles.Should().BeEquivalentTo(["technician", "maintenance-planner", "offline_access"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"roles":"technician"}""")]
    [InlineData("""{"roles":[1,null,""]}""")]
    public void Malformed_or_empty_realm_access_yields_no_roles(string json)
    {
        KeycloakRoles.ParseRoles(json).Should().BeEmpty();
    }

    [Fact]
    public void Lifts_realm_roles_to_standard_role_claims_without_duplicates()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", """{"roles":["technician","administrator"]}"""),
            new Claim(ClaimTypes.Role, "technician"),
        ], "test");
        var principal = new ClaimsPrincipal(identity);

        KeycloakRoles.LiftRealmRoles(principal);

        principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo(["technician", "administrator"]);
        principal.IsInRole(Roles.Administrator).Should().BeTrue();
    }

    [Fact]
    public void DescribeCaller_reads_username_tenant_and_sorted_roles()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "11111111-2222-3333-4444-555555555555"),
            new Claim("preferred_username", "planner"),
            new Claim("tenant_id", "tnt-alsafa"),
            new Claim(ClaimTypes.Role, "maintenance-planner"),
            new Claim(ClaimTypes.Role, "administrator"),
        ], "test", nameType: "preferred_username", roleType: ClaimTypes.Role);

        var info = PlatformAuthenticationExtensions.DescribeCaller(new ClaimsPrincipal(identity));

        info.Should().BeEquivalentTo(new CallerInfo(
            "11111111-2222-3333-4444-555555555555", "planner", "tnt-alsafa", ["administrator", "maintenance-planner"]));
    }
}
