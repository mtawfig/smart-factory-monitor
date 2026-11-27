using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartFactory.BuildingBlocks.Auditing;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Audit.Domain;

namespace SmartFactory.Modules.Audit.Persistence;

/// <summary>
/// Implements the <see cref="IAuditTrail"/> contract. Writes on the caller's connection and inside the caller's
/// transaction, so the audit records commit or roll back with the business change.
/// </summary>
internal sealed class AuditTrail(ICurrentTenant tenant, ICurrentActor actor, IHttpContextAccessor http) : IAuditTrail
{
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web);

    public async Task RecordAsync(IReadOnlyList<AuditRecord> records, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count == 0)
        {
            return;
        }

        var tenantId = tenant.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("Audit records need a tenant in the current scope.");
        }

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new AuditDbContext(options, tenant);
        await db.Database.UseTransactionAsync(transaction, cancellationToken).ConfigureAwait(false);

        // Why an advisory lock: two concurrent requests of the same tenant must not both read sequence N and both
        // write N+1. The lock is per tenant and released automatically at commit or rollback.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({tenantId}, 0))", cancellationToken).ConfigureAwait(false);

        var last = await db.Events.AsNoTracking()
            .OrderByDescending(e => e.Sequence)
            .Select(e => new { e.Sequence, e.ChainHash })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        var sequence = last?.Sequence ?? 0;
        var previous = last?.ChainHash;
        var occurredAt = DateTimeOffset.UtcNow;
        var correlationId = Activity.Current?.TraceId.ToHexString() ?? http.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        foreach (var record in records)
        {
            var auditEvent = new AuditEvent(
                tenantId, ++sequence, occurredAt, actor.Id, actor.DisplayName,
                record.AggregateType, record.AggregateId, record.Action,
                Serialize(record.Before), Serialize(record.After), correlationId, previous);
            db.Events.Add(auditEvent);
            previous = auditEvent.ChainHash;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? Serialize(object? snapshot) => snapshot is null ? null : JsonSerializer.Serialize(snapshot, snapshot.GetType(), SnapshotJson);
}
