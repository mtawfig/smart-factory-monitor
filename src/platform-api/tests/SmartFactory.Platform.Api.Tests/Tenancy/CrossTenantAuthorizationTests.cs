using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Platform.Api.Tests.Infrastructure;
using SmartFactory.Platform.Api.Tests.Maintenance;

namespace SmartFactory.Platform.Api.Tests.Tenancy;

/// <summary>
/// ADR-008: every endpoint is tested from the wrong tenant. Both tenants hold the same plant with the same asset
/// codes, so a leak would be easy to miss by eye; the tests compare ids. A foreign id must look like it does not exist
/// (404), never 403, so the API does not even confirm that another tenant's record exists.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class CrossTenantAuthorizationTests(PlatformApiFactory factory)
{
    /// <summary>Every tenant-scoped route and how this class attacks it. A new endpoint must be added here.</summary>
    private static readonly IReadOnlySet<string> CoveredRoutes = new HashSet<string>
    {
        "GET /api/assets/plants",
        "GET /api/assets/assets",
        "GET /api/assets/assets/{id:guid}",
        "POST /api/assets/assets",
        "PUT /api/assets/assets/{id:guid}",
        "POST /api/assets/assets/{id:guid}/sensors",
        "DELETE /api/assets/assets/{id:guid}/sensors/{sensorId:guid}",
        "GET /api/audit/events",
        "GET /api/audit/verify",
        "POST /api/maintenance/requests",
        "GET /api/maintenance/work-orders",
        "GET /api/maintenance/work-orders/{id:guid}",
        "PUT /api/maintenance/work-orders/{id:guid}",
        "POST /api/maintenance/work-orders/{id:guid}/approve",
        "POST /api/maintenance/work-orders/{id:guid}/reject",
        "POST /api/maintenance/work-orders/{id:guid}/schedule",
        "POST /api/maintenance/work-orders/{id:guid}/start",
        "POST /api/maintenance/work-orders/{id:guid}/hold",
        "POST /api/maintenance/work-orders/{id:guid}/resume",
        "POST /api/maintenance/work-orders/{id:guid}/complete",
        "POST /api/maintenance/work-orders/{id:guid}/close",
        "POST /api/maintenance/work-orders/{id:guid}/cancel",
        "PUT /api/maintenance/work-orders/{id:guid}/checklist/{itemId:guid}",
        "POST /api/maintenance/work-orders/{id:guid}/findings",
        "POST /api/maintenance/work-orders/{id:guid}/findings/{findingId:guid}/attachments",
        "GET /api/maintenance/attachments/{id:guid}",
    };

    private static readonly string[] TenantScopedPrefixes = ["/api/assets", "/api/audit", "/api/maintenance"];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Every_tenant_scoped_endpoint_is_covered_by_a_cross_tenant_test()
    {
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } raw && TenantScopedPrefixes.Any(p => raw.StartsWith(p, StringComparison.Ordinal)))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).Select(m => $"{m} {e.RoutePattern.RawText}"))
            .ToHashSet();

        routes.Should().BeEquivalentTo(CoveredRoutes, "each tenant-scoped endpoint needs a cross-tenant test (ADR-008)");
    }

    [Fact]
    public async Task Lists_never_contain_the_other_tenants_records()
    {
        factory.RequireDatabase();
        var a = factory.CreateClient().As(TestUsers.TenantA, Roles.Administrator);
        var b = factory.CreateClient().As(TestUsers.TenantB, Roles.Administrator);

        var assetsA = (await a.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets", Ct))!.Select(x => x.Id).ToHashSet();
        var assetsB = (await b.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets", Ct))!.Select(x => x.Id).ToHashSet();
        var plantsA = (await a.GetFromJsonAsync<List<PlantModel>>("/api/assets/plants", Ct))!.Select(x => x.Id);
        var plantsB = (await b.GetFromJsonAsync<List<PlantModel>>("/api/assets/plants", Ct))!.Select(x => x.Id);
        var auditA = (await a.GetFromJsonAsync<List<AuditEventModel>>("/api/audit/events?take=500", Ct))!.Select(x => x.AggregateId).ToHashSet();

        assetsA.Should().NotBeEmpty().And.NotIntersectWith(assetsB);
        plantsA.Should().NotIntersectWith(plantsB);
        auditA.Should().NotIntersectWith(assetsB.Select(id => id.ToString()));
    }

    [Fact]
    public async Task Foreign_ids_are_invisible_to_reads_and_writes()
    {
        factory.RequireDatabase();
        var a = factory.CreateClient().As(TestUsers.TenantA, Roles.Administrator);
        var attacker = factory.CreateClient().As(TestUsers.TenantB, Roles.Administrator);
        var victim = (await a.GetFromJsonAsync<List<AssetSummaryModel>>("/api/assets/assets?search=MTR-042", Ct))!.Single();
        var victimDetail = (await a.GetFromJsonAsync<AssetDetailModel>($"/api/assets/assets/{victim.Id}", Ct))!;
        var victimSensor = victimDetail.Sensors[0];

        var read = await attacker.GetAsync($"/api/assets/assets/{victim.Id}", Ct);
        var update = await attacker.PutAsJsonAsync($"/api/assets/assets/{victim.Id}",
            new { name = "pwned", criticality = "Low", status = "Decommissioned", description = (string?)null, ratedPowerKw = (decimal?)null, version = victimDetail.Version }, Ct);
        var addSensor = await attacker.PostAsJsonAsync($"/api/assets/assets/{victim.Id}/sensors",
            new { metric = "evil", unit = "x", intervalSeconds = 5m }, Ct);
        var removeSensor = await attacker.DeleteAsync($"/api/assets/assets/{victim.Id}/sensors/{victimSensor.Id}", Ct);
        var createOnForeignLine = await attacker.PostAsJsonAsync("/api/assets/assets",
            new { lineId = victim.LineId, code = "FAN-666", name = "Planted", assetClass = "FAN", criticality = "Low" }, Ct);
        var foreignAudit = await attacker.GetFromJsonAsync<List<AuditEventModel>>($"/api/audit/events?aggregateId={victim.Id}", Ct);

        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
        addSensor.StatusCode.Should().Be(HttpStatusCode.NotFound);
        removeSensor.StatusCode.Should().Be(HttpStatusCode.NotFound);
        createOnForeignLine.StatusCode.Should().Be(HttpStatusCode.NotFound);
        foreignAudit.Should().BeEmpty();

        // And nothing changed on the victim's side.
        var after = (await a.GetFromJsonAsync<AssetDetailModel>($"/api/assets/assets/{victim.Id}", Ct))!;
        after.Name.Should().Be(victimDetail.Name);
        after.Version.Should().Be(victimDetail.Version);
        after.Sensors.Should().HaveCount(victimDetail.Sensors.Count);
    }

    [Fact]
    public async Task Audit_verification_covers_only_the_callers_tenant()
    {
        factory.RequireDatabase();
        var b = factory.CreateClient().As(TestUsers.TenantB, Roles.Administrator);

        var verification = await b.GetFromJsonAsync<AuditVerificationModel>("/api/audit/verify", Ct);

        verification!.Intact.Should().BeTrue();
        verification.EventsChecked.Should().BeGreaterThanOrEqualTo(44, "tenant B was seeded with the same 44 records");
    }

    [Fact]
    public async Task Another_tenants_work_orders_findings_and_attachments_do_not_exist()
    {
        factory.RequireDatabase();
        // Tenant A: a work order in progress with a finding and an attachment, assigned to tech.one.
        var tech = factory.Technician();
        var w = await factory.ScheduledAsync(Ct);
        w = await tech.ActAsync(w.Id, "start", new { version = w.Version }, Ct);
        var finding = (await (await tech.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/findings",
            new { condition = "Worn", notes = "Tenant A evidence." }, Ct)).Content.ReadFromJsonAsync<FindingModel>(Ct))!;
        var attachment = (await (await tech.PostAsync($"/api/maintenance/work-orders/{w.Id}/findings/{finding.Id}/attachments",
            Crew.FilePart(Crew.Png, "a.png", "image/png"), Ct)).Content.ReadFromJsonAsync<AttachmentModel>(Ct))!;
        var tenantAAsset = await factory.AssetIdAsync("MTR-042", TestUsers.TenantA, Ct);

        // Tenant B: same usernames and roles, including an admin and a technician with the same name.
        var admin = factory.CreateClient().AsUser(TestUsers.TenantB, "planner", Roles.Administrator, Roles.MaintenancePlanner, Roles.Technician);
        var techB = factory.Technician(Crew.Tech1, TestUsers.TenantB);
        var path = $"/api/maintenance/work-orders/{w.Id}";
        var v = new { version = w.Version, reason = "x", assignee = Crew.Tech1, scheduledFor = "2026-11-05", disposition = "Repaired" };

        var responses = new List<(string What, HttpResponseMessage Response)>
        {
            ("get", await admin.GetAsync(path, Ct)),
            ("edit", await admin.PutAsJsonAsync(path, new { version = w.Version, title = "pwned", description = (string?)null, priority = "Low", dueDate = (string?)null, checklist = Array.Empty<string>() }, Ct)),
            ("checklist", await techB.PutAsJsonAsync($"{path}/checklist/{w.Checklist[0].Id}", new { version = w.Version, status = "Done", note = (string?)null }, Ct)),
            ("finding", await techB.PostAsJsonAsync($"{path}/findings", new { condition = "Failed", notes = "planted evidence" }, Ct)),
            ("upload", await techB.PostAsync($"{path}/findings/{finding.Id}/attachments", Crew.FilePart(Crew.Png, "b.png", "image/png"), Ct)),
            ("download", await admin.GetAsync($"/api/maintenance/attachments/{attachment.Id}", Ct)),
            ("request on foreign asset", await admin.PostAsJsonAsync("/api/maintenance/requests", new { assetId = tenantAAsset, title = "Planted", reason = "Planted request", priority = "Low" }, Ct)),
        };
        foreach (var action in new[] { "approve", "reject", "schedule", "start", "hold", "resume", "complete", "close", "cancel" })
        {
            responses.Add((action, await (action is "start" or "complete" ? techB : admin).PostAsJsonAsync($"{path}/{action}", v, Ct)));
        }

        var listB = await admin.GetFromJsonAsync<List<WorkOrderSummaryModel>>("/api/maintenance/work-orders", Ct);

        responses.Should().OnlyContain(r => r.Response.StatusCode == HttpStatusCode.NotFound,
            string.Join(", ", responses.Where(r => r.Response.StatusCode != HttpStatusCode.NotFound).Select(r => $"{r.What}={(int)r.Response.StatusCode}")));
        listB.Should().NotContain(x => x.Id == w.Id);
        var after = (await tech.GetFromJsonAsync<WorkOrderDetailModel>(path, Ct))!;
        after.Revision.Should().Be(w.Revision + 1, "only tenant A's own finding changed it");
        after.Findings.Should().ContainSingle().Which.Attachments.Should().ContainSingle();
    }

    [Fact]
    public async Task A_token_without_a_tenant_claim_is_forbidden()
    {
        factory.RequireDatabase();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "someone");
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, Roles.Administrator);

        var response = await client.GetAsync("/api/assets/assets", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
