using System.Net.Http.Json;
using Npgsql;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Modules.Assets;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests.Audit;

[Collection(DatabaseTestGroup.Name)]
public sealed class AuditTrailTests(PlatformApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Seeding_is_idempotent_and_audited_once()
    {
        factory.RequireDatabase();

        var createdAgain = await factory.Services.SeedAssetsFromPlantSpecAsync(RepositoryPaths.PlantSpec, TestUsers.TenantA, Ct);
        var seedEvents = await factory.CreateClient().As(TestUsers.TenantA, Roles.Administrator)
            .GetFromJsonAsync<List<AuditEventModel>>("/api/audit/events?aggregateType=SensorMapping&take=500", Ct);

        createdAgain.Should().Be(0);
        seedEvents!.Where(e => e.ActorId == "system:seed").Should().HaveCount(32);
    }

    [Fact]
    public async Task The_hash_chain_verifies_and_detects_a_tampered_record()
    {
        factory.RequireDatabase();
        const string tenant = "tnt-tamper";
        await factory.Services.SeedAssetsFromPlantSpecAsync(RepositoryPaths.PlantSpec, tenant, Ct);
        var admin = factory.CreateClient().As(tenant, Roles.Administrator);

        var before = await admin.GetFromJsonAsync<AuditVerificationModel>("/api/audit/verify", Ct);
        before.Should().Be(new AuditVerificationModel(true, 44, null));

        // Simulate an attacker with database-owner rights: switch the trigger off, edit history, switch it back on.
        await using (var owner = new NpgsqlConnection(factory.ConnectionString))
        {
            await owner.OpenAsync(Ct);
            await using var tamper = new NpgsqlCommand("""
                ALTER TABLE audit.audit_events DISABLE TRIGGER audit_events_append_only;
                UPDATE audit.audit_events SET after_state = replace(after_state, 'High', 'Low')
                 WHERE tenant_id = 'tnt-tamper' AND aggregate_type = 'Asset' AND after_state LIKE '%MTR-042%';
                ALTER TABLE audit.audit_events ENABLE TRIGGER audit_events_append_only;
                """, owner);
            await tamper.ExecuteNonQueryAsync(Ct);
        }

        var after = await admin.GetFromJsonAsync<AuditVerificationModel>("/api/audit/verify", Ct);

        after!.Intact.Should().BeFalse();
        after.FirstBrokenSequence.Should().NotBeNull().And.BeGreaterThan(1);
    }
}
