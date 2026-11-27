using Microsoft.Extensions.Options;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets.Domain;
using SmartFactory.Modules.Assets.Endpoints;
using SmartFactory.Modules.Assets.Seeding;
using SmartFactory.Modules.Audit.Domain;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests;

/// <summary>Fast tests with no host and no database.</summary>
public sealed class UnitTests
{
    [Fact]
    public void Plant_spec_loads_eight_assets_and_thirty_two_sensors_with_criticality()
    {
        var spec = PlantSpecification.Load(RepositoryPaths.PlantSpec);

        var assets = spec.Lines.SelectMany(l => l.Assets).ToList();
        assets.Should().HaveCount(8);
        assets.Sum(a => a.Sensors.Count).Should().Be(32);
        assets.Should().OnlyContain(a => AssetValidation.IsName<Criticality>(a.Criticality));
        assets.Should().OnlyContain(a => Asset.IsValidCode(a.Id) && AssetClasses.All.Contains(a.AssetClass));
        assets.SelectMany(a => a.Sensors).Should().OnlyContain(s => SensorMapping.IsValidMetric(s.Metric) && s.IntervalS > 0);
    }

    [Fact]
    public void Topic_follows_adr004()
    {
        SensorMapping.BuildTopic("tnt-alsafa", "plant-01", "line-b", "PMP-211", "flow")
            .Should().Be("plant/tnt-alsafa/plant-01/line-b/PMP-211/flow");
    }

    [Theory]
    [InlineData("MTR-042", true)]
    [InlineData("mtr-042", false)]
    [InlineData("MTR-42", false)]
    [InlineData("MOTOR-042", false)]
    [InlineData(null, false)]
    public void Asset_codes_follow_the_plant_convention(string? code, bool valid) => Asset.IsValidCode(code).Should().Be(valid);

    [Fact]
    public void Enum_validation_accepts_names_but_not_numbers()
    {
        AssetValidation.IsName<Criticality>("High").Should().BeTrue();
        AssetValidation.IsName<Criticality>("high").Should().BeFalse();
        AssetValidation.IsName<Criticality>("2").Should().BeFalse();
    }

    [Fact]
    public void Audit_chain_is_deterministic_and_detects_any_change()
    {
        var at = new DateTimeOffset(2026, 11, 2, 10, 8, 2, 123, TimeSpan.Zero).AddTicks(4567);
        var first = new AuditEvent("tnt-a", 1, at, "u1", "User One", "Asset", "id-1", "Created", null, "{\"name\":\"Motor\"}", "corr", null);
        var second = new AuditEvent("tnt-a", 2, at, "u1", "User One", "Asset", "id-1", "Updated", "{\"name\":\"Motor\"}", "{\"name\":\"Pump\"}", "corr", first.ChainHash);
        var replay = new AuditEvent("tnt-a", 1, at, "u1", "User One", "Asset", "id-1", "Created", null, "{\"name\":\"Motor\"}", "corr", null);

        replay.ChainHash.Should().Be(first.ChainHash, "same input, same hash");
        first.OccurredAtUtc.Ticks.Should().Be(at.Ticks - (at.Ticks % 10), "truncated to PostgreSQL microseconds");
        first.IsIntact(null).Should().BeTrue();
        second.IsIntact(first.ChainHash).Should().BeTrue();
        second.IsIntact("0000000000000000000000000000000000000000000000000000000000000000").Should().BeFalse("wrong predecessor");
        second.BeforeHash.Should().Be(first.AfterHash, "the same snapshot text hashes the same");
    }

    [Fact]
    public void Tenant_scopes_nest_and_restore()
    {
        TenantScope.Current.Should().BeNull();
        using (TenantScope.Begin("tnt-outer"))
        {
            TenantScope.Current.Should().Be("tnt-outer");
            using (TenantScope.Begin("tnt-inner"))
            {
                TenantScope.Current.Should().Be("tnt-inner");
            }

            TenantScope.Current.Should().Be("tnt-outer");
        }

        TenantScope.Current.Should().BeNull();
    }

    [Theory]
    [InlineData("sf_app; DROP TABLE x")]
    [InlineData("SF_APP")]
    [InlineData("sf-app")]
    public async Task Session_interceptor_refuses_unsafe_role_names(string role)
    {
        var interceptor = new TenantSessionInterceptor(new FixedTenant("tnt-a"), Options.Create(new DatabaseOptions { ApplicationRole = role }));
        await using var connection = new Npgsql.NpgsqlConnection("Host=unused");

        var act = async () => await interceptor.ConnectionOpenedAsync(connection, null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not a valid role name*");
    }

    private sealed class FixedTenant(string tenant) : ICurrentTenant
    {
        public string? TenantId => tenant;
    }
}
