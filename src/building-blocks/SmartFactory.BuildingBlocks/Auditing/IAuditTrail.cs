using System.Data.Common;

namespace SmartFactory.BuildingBlocks.Auditing;

/// <summary>
/// One state change to record. <paramref name="Before"/> and <paramref name="After"/> are snapshots (plain DTOs) of
/// the aggregate; either may be null (create / delete).
/// </summary>
public sealed record AuditRecord(string AggregateType, string AggregateId, string Action, object? Before, object? After);

/// <summary>
/// The contract modules use to write audit events (ADR-002: modules depend on contracts, not on each other). The
/// Audit module implements it. Records are written inside the caller's open transaction, so a business change and
/// its audit trail commit or roll back together.
/// </summary>
public interface IAuditTrail
{
    Task RecordAsync(IReadOnlyList<AuditRecord> records, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
}
