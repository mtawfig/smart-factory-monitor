using Npgsql;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests.Tenancy;

/// <summary>
/// The database layer of ADR-008, tested with raw SQL that bypasses EF Core entirely: even code that forgets every
/// application-level filter cannot read or write another tenant's rows while running as the application role.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class RowLevelSecurityTests(PlatformApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null, 0)]
    [InlineData(TestUsers.TenantA, 8)]
    [InlineData(TestUsers.TenantB, 8)]
    [InlineData("tnt-nobody", 0)]
    public async Task Raw_sql_as_the_application_role_sees_only_the_session_tenant(string? tenant, long expectedSeededAssets)
    {
        factory.RequireDatabase();
        await using var connection = await OpenAsApplicationAsync(tenant);

        var codes = await QueryAsync(connection, "SELECT code FROM assets.assets WHERE code IN ('MTR-042','CVR-012','CMP-007','FAN-034','CNC-018','PMP-211','PRS-105','CHL-003')");
        var foreignTenants = await QueryAsync(connection, "SELECT DISTINCT tenant_id FROM assets.sensor_mappings");

        codes.Should().HaveCount((int)expectedSeededAssets);
        foreignTenants.Should().OnlyContain(t => t == tenant);
    }

    [Fact]
    public async Task Inserting_a_row_for_another_tenant_is_rejected_by_the_policy()
    {
        factory.RequireDatabase();
        await using var connection = await OpenAsApplicationAsync(TestUsers.TenantA);

        var act = async () =>
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO assets.plants (id, tenant_id, code, name, time_zone) VALUES (gen_random_uuid(), @t, 'evil', 'Evil', 'UTC')", connection);
            insert.Parameters.AddWithValue("t", TestUsers.TenantB);
            await insert.ExecuteNonQueryAsync(Ct);
        };

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task The_application_role_cannot_rewrite_or_delete_audit_events()
    {
        factory.RequireDatabase();
        await using var connection = await OpenAsApplicationAsync(TestUsers.TenantA);

        var update = async () => await new NpgsqlCommand("UPDATE audit.audit_events SET action = 'x'", connection).ExecuteNonQueryAsync(Ct);
        var delete = async () => await new NpgsqlCommand("DELETE FROM audit.audit_events", connection).ExecuteNonQueryAsync(Ct);

        (await update.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        (await delete.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Even_the_owner_cannot_delete_audit_events_because_of_the_trigger()
    {
        factory.RequireDatabase();
        await using var owner = new NpgsqlConnection(factory.ConnectionString);
        await owner.OpenAsync(Ct);

        var delete = async () => await new NpgsqlCommand("DELETE FROM audit.audit_events WHERE sequence = 1", owner).ExecuteNonQueryAsync(Ct);

        (await delete.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("append-only");
    }

    [Theory]
    [InlineData("UPDATE maintenance.work_order_history SET note = 'rewritten'")]
    [InlineData("DELETE FROM maintenance.work_order_history")]
    [InlineData("UPDATE maintenance.findings SET notes = 'rewritten'")]
    [InlineData("DELETE FROM maintenance.attachments")]
    [InlineData("DELETE FROM maintenance.work_orders")]
    public async Task Maintenance_evidence_and_work_orders_cannot_be_rewritten_or_deleted_by_the_application(string sql)
    {
        factory.RequireDatabase();
        await using var connection = await OpenAsApplicationAsync(TestUsers.TenantA);

        var act = async () => await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync(Ct);

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Maintenance_rows_are_isolated_by_tenant_in_the_database()
    {
        factory.RequireDatabase();
        await using var a = await OpenAsApplicationAsync(TestUsers.TenantA);
        await using var none = await OpenAsApplicationAsync(null);

        var tenantsSeenByA = await QueryAsync(a, "SELECT DISTINCT tenant_id FROM maintenance.work_orders");
        var seenWithoutTenant = await QueryAsync(none, "SELECT id::text FROM maintenance.work_orders");

        tenantsSeenByA.Should().OnlyContain(t => t == TestUsers.TenantA);
        seenWithoutTenant.Should().BeEmpty();
    }

    private async Task<NpgsqlConnection> OpenAsApplicationAsync(string? tenant)
    {
        var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var session = new NpgsqlCommand("SET ROLE sf_app; SELECT set_config('app.tenant_id', @t, false);", connection);
        session.Parameters.AddWithValue("t", tenant ?? string.Empty);
        await session.ExecuteNonQueryAsync(Ct);
        return connection;
    }

    private static async Task<List<string>> QueryAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        var values = new List<string>();
        while (await reader.ReadAsync(Ct))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
