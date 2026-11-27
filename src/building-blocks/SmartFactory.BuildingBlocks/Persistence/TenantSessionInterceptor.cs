using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.BuildingBlocks.Persistence;

/// <summary>
/// Runs on every physical connection open. It (1) switches to the unprivileged application role so PostgreSQL
/// row-level security applies, and (2) writes the current tenant into the session variable <c>app.tenant_id</c>
/// that every RLS policy reads. With no tenant the variable is empty and RLS returns no tenant rows at all:
/// failing closed, not open.
/// </summary>
public sealed class TenantSessionInterceptor(ICurrentTenant tenant, IOptions<DatabaseOptions> options) : DbConnectionInterceptor
{
    public const string TenantSetting = "app.tenant_id";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        var role = options.Value.ApplicationRole;
        var switchRole = !string.IsNullOrWhiteSpace(role) && !ElevatedDatabaseScope.IsActive;

        // Why a quoted identifier and not a parameter: SET ROLE does not accept parameters. The role name comes
        // from configuration, never from a request, and is validated to a safe identifier shape below.
        if (switchRole && !IsSafeIdentifier(role))
        {
            throw new InvalidOperationException($"Database:ApplicationRole '{role}' is not a valid role name.");
        }

        command.CommandText = (switchRole ? $"SET ROLE \"{role}\"; " : "RESET ROLE; ")
                              + $"SELECT set_config('{TenantSetting}', @tenant, false);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";
        parameter.Value = tenant.TenantId ?? string.Empty;
        command.Parameters.Add(parameter);
        return command;
    }

    private static bool IsSafeIdentifier(string value) =>
        value.Length is > 0 and <= 63 && value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_');
}
