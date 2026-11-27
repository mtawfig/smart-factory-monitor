using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.Modules.Audit.Domain;

/// <summary>
/// One immutable audit record. Records form a per-tenant chain: each <see cref="ChainHash"/> covers the record's own
/// fields and the previous record's chain hash, so editing, deleting or reordering any past record breaks every hash
/// after it. The database additionally rejects UPDATE, DELETE and TRUNCATE on the table (append-only trigger).
/// </summary>
internal sealed class AuditEvent : ITenantScoped
{
    private AuditEvent() { }

    public AuditEvent(
        string tenantId, long sequence, DateTimeOffset occurredAtUtc, string actorId, string actorName,
        string aggregateType, string aggregateId, string action, string? beforeState, string? afterState,
        string correlationId, string? previousChainHash)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        Sequence = sequence;
        OccurredAtUtc = TruncateToMicroseconds(occurredAtUtc);
        ActorId = actorId;
        ActorName = actorName;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        Action = action;
        BeforeState = beforeState;
        AfterState = afterState;
        BeforeHash = Hash(beforeState);
        AfterHash = Hash(afterState);
        CorrelationId = correlationId;
        PreviousChainHash = previousChainHash;
        ChainHash = ComputeChainHash();
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public long Sequence { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string ActorName { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public string AggregateId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;

    /// <summary>JSON snapshot before the change, stored as exact text so its hash can be re-verified byte for byte.</summary>
    public string? BeforeState { get; private set; }

    public string? AfterState { get; private set; }
    public string? BeforeHash { get; private set; }
    public string? AfterHash { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? PreviousChainHash { get; private set; }
    public string ChainHash { get; private set; } = string.Empty;

    /// <summary>Recomputes all three hashes from the stored fields; false means the record or its chain was altered.</summary>
    public bool IsIntact(string? expectedPreviousChainHash) =>
        string.Equals(PreviousChainHash, expectedPreviousChainHash, StringComparison.Ordinal)
        && string.Equals(BeforeHash, Hash(BeforeState), StringComparison.Ordinal)
        && string.Equals(AfterHash, Hash(AfterState), StringComparison.Ordinal)
        && string.Equals(ChainHash, ComputeChainHash(), StringComparison.Ordinal);

    internal string ComputeChainHash()
    {
        // Why a newline-joined, fixed field order with invariant formatting: the same input must produce the same
        // bytes on every machine and every future .NET version, or verification would report false tampering.
        var material = string.Join('\n',
            PreviousChainHash ?? "-",
            TenantId,
            Sequence.ToString(CultureInfo.InvariantCulture),
            OccurredAtUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture),
            ActorId,
            AggregateType,
            AggregateId,
            Action,
            BeforeHash ?? "-",
            AfterHash ?? "-",
            CorrelationId);
        return Sha256(material);
    }

    internal static string? Hash(string? state) => state is null ? null : Sha256(state);

    private static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    // Why: PostgreSQL timestamptz keeps microseconds; .NET ticks are 100 ns. Hashing the untruncated value would make
    // every record look tampered with after a round trip through the database.
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - (utc.Ticks % 10), TimeSpan.Zero);
    }
}
