using System.Net;
using System.Net.Http.Json;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Modules.Assets;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests.Assets;

/// <summary>Behaviour of the Assets endpoints for users of one tenant, against the seeded plant (synthetic data).</summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class AssetsApiTests(PlatformApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly HashSet<string> SeededCodes = ["MTR-042", "CVR-012", "CMP-007", "FAN-034", "CNC-018", "PMP-211", "PRS-105", "CHL-003"];

    private HttpClient Planner() => factory.CreateClient().As(TestUsers.TenantA, Roles.MaintenancePlanner);

    private HttpClient Admin() => factory.CreateClient().As(TestUsers.TenantA, Roles.Administrator);

    [Fact]
    public async Task Plants_list_the_seeded_plant_with_both_lines()
    {
        factory.RequireDatabase();
        // Why a fresh tenant: other tests in this collection add assets to tenant A, so exact counts need a clean plant.
        const string tenant = "tnt-plants";
        await factory.Services.SeedAssetsFromPlantSpecAsync(RepositoryPaths.PlantSpec, tenant, Ct);

        var plants = await factory.CreateClient().As(tenant, Roles.MaintenancePlanner).GetFromJsonAsync<List<PlantModel>>("/api/assets/plants", Ct);

        plants.Should().ContainSingle().Which.Code.Should().Be("plant-01");
        plants![0].Lines.Select(l => (l.Code, l.AssetCount)).Should().Equal(("line-a", 4), ("line-b", 4));
    }

    [Fact]
    public async Task Assets_list_all_eight_machines_and_filter_by_line_and_text()
    {
        factory.RequireDatabase();
        var client = Planner();

        // Why "seeded" filtering: other tests in this collection add assets to the same tenant.
        var all = (await client.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets", Ct))!;
        var seeded = all.Where(a => SeededCodes.Contains(a.Code)).ToList();
        seeded.Should().HaveCount(8);
        seeded.Sum(a => a.SensorCount).Should().Be(32);

        var lineB = seeded.First(a => a.LineCode == "line-b").LineId;
        var machining = await client.GetFromJsonAsync<List<AssetSummaryModel>>($"/api/assets/assets?lineId={lineB}", Ct);
        machining!.Where(a => SeededCodes.Contains(a.Code)).Select(a => a.Code).Should().BeEquivalentTo(["CNC-018", "PMP-211", "PRS-105", "CHL-003"]);
        machining.Should().OnlyContain(a => a.LineCode == "line-b");

        var search = await client.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets?search=mtr", Ct);
        search.Should().ContainSingle().Which.Name.Should().Be("Filler drive motor");

        var high = await client.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets?criticality=High", Ct);
        high!.Where(a => SeededCodes.Contains(a.Code)).Select(a => a.Code).Should().BeEquivalentTo(["MTR-042", "CMP-007", "CNC-018", "PRS-105"]);
        high.Should().OnlyContain(a => a.Criticality == "High");
    }

    [Fact]
    public async Task Asset_detail_returns_sensors_with_adr004_topics()
    {
        factory.RequireDatabase();
        var client = Planner();
        var motor = (await client.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets?search=MTR-042", Ct))!.Single();

        var detail = await client.GetFromJsonAsync<AssetDetailModel>($"/api/assets/assets/{motor.Id}", Ct);

        detail!.Criticality.Should().Be("High");
        detail.RatedPowerKw.Should().Be(15);
        detail.Sensors.Select(s => s.Metric).Should().BeEquivalentTo(["bearing_temp", "current", "speed", "vibration_rms"]);
        var vibration = detail.Sensors.Single(s => s.Metric == "vibration_rms");
        vibration.SourceTopic.Should().Be("plant/tnt-alsafa/plant-01/line-a/MTR-042/vibration_rms");
        (vibration.NominalMin, vibration.NominalMax).Should().Be((1.0m, 2.8m));
    }

    [Fact]
    public async Task Unknown_asset_is_404()
    {
        factory.RequireDatabase();

        var response = await Planner().GetAsync($"/api/assets/assets/{Guid.NewGuid()}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Administrator_creates_updates_and_maps_sensors_with_audit_and_concurrency_control()
    {
        factory.RequireDatabase();
        var admin = Admin();
        var lineA = (await admin.GetFromJsonAsync<List<PlantModel>>("/api/assets/plants", Ct))![0].Lines.Single(l => l.Code == "line-a");

        // Create
        var create = await admin.PostAsJsonAsync("/api/assets/assets",
            new { lineId = lineA.Id, code = "PMP-901", name = "Wash-down pump", assetClass = "PUMP", criticality = "Low", description = "Test asset", ratedPowerKw = 2.2m }, Ct);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<AssetDetailModel>(Ct))!;
        created.Status.Should().Be("InService");

        // Duplicate code in the same tenant
        var duplicate = await admin.PostAsJsonAsync("/api/assets/assets",
            new { lineId = lineA.Id, code = "PMP-901", name = "Again", assetClass = "PUMP", criticality = "Low" }, Ct);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Update with the current version succeeds and changes the version
        var update = await admin.PutAsJsonAsync($"/api/assets/assets/{created.Id}",
            new { name = "Wash-down pump (spare)", criticality = "Medium", status = "OutOfService", description = (string?)null, ratedPowerKw = 2.2m, version = created.Version }, Ct);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<AssetDetailModel>(Ct))!;
        updated.Version.Should().NotBe(created.Version);
        updated.Status.Should().Be("OutOfService");

        // Update with the stale version is rejected instead of silently overwriting
        var stale = await admin.PutAsJsonAsync($"/api/assets/assets/{created.Id}",
            new { name = "Lost update", criticality = "High", status = "InService", description = (string?)null, ratedPowerKw = 2.2m, version = created.Version }, Ct);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Map and remove a sensor
        var sensor = await admin.PostAsJsonAsync($"/api/assets/assets/{created.Id}/sensors",
            new { metric = "flow", unit = "L/min", intervalSeconds = 5m, nominalMin = 10m, nominalMax = 20m, kind = (string?)null }, Ct);
        sensor.StatusCode.Should().Be(HttpStatusCode.Created);
        var mapped = (await sensor.Content.ReadFromJsonAsync<SensorModel>(Ct))!;
        mapped.SourceTopic.Should().Be("plant/tnt-alsafa/plant-01/line-a/PMP-901/flow");
        (await admin.DeleteAsync($"/api/assets/assets/{created.Id}/sensors/{mapped.Id}", Ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Every change is in the audit trail, with before/after hashes on the update
        var audit = await admin.GetFromJsonAsync<List<AuditEventModel>>($"/api/audit/events?aggregateType=Asset&aggregateId={created.Id}", Ct);
        audit!.Select(e => e.Action).Should().Equal("Updated", "Created");
        var updatedEvent = audit![0];
        updatedEvent.BeforeHash!.Should().HaveLength(64);
        updatedEvent.AfterHash!.Should().HaveLength(64).And.NotBe(updatedEvent.BeforeHash);
        updatedEvent.ActorId.Should().Be($"{Roles.Administrator}@{TestUsers.TenantA}");
        updatedEvent.AfterState.Should().Contain("OutOfService");
    }

    [Fact]
    public async Task Invalid_requests_return_validation_problems()
    {
        factory.RequireDatabase();
        var admin = Admin();
        var lineA = (await admin.GetFromJsonAsync<List<PlantModel>>("/api/assets/plants", Ct))![0].Lines[0];

        var response = await admin.PostAsJsonAsync("/api/assets/assets",
            new { lineId = lineA.Id, code = "bad-code", name = "", assetClass = "TOASTER", criticality = "7" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.Should().Contain("code").And.Contain("name").And.Contain("assetClass").And.Contain("criticality");
    }

    [Theory]
    [InlineData(Roles.MaintenancePlanner)]
    [InlineData(Roles.Technician)]
    [InlineData(Roles.ReliabilityManager)]
    [InlineData(Roles.MlEngineer)]
    public async Task Only_administrators_may_change_assets(string role)
    {
        factory.RequireDatabase();
        var client = factory.CreateClient().As(TestUsers.TenantA, role);
        var anyAsset = (await client.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets", Ct))![0];

        var create = await client.PostAsJsonAsync("/api/assets/assets",
            new { lineId = anyAsset.LineId, code = "FAN-777", name = "Nope", assetClass = "FAN", criticality = "Low" }, Ct);
        var sensor = await client.PostAsJsonAsync($"/api/assets/assets/{anyAsset.Id}/sensors",
            new { metric = "nope", unit = "x", intervalSeconds = 5m }, Ct);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        sensor.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
