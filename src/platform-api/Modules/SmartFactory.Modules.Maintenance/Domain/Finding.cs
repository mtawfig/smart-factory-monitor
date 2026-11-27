using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.Modules.Maintenance.Domain;

/// <summary>What the technician saw. Becomes the outcome label for model evaluation later (blueprint: closed loop).</summary>
internal enum ConditionLabel { Good, Worn, Degraded, Failed }

/// <summary>An observation recorded by the assigned technician during the work.</summary>
internal sealed class Finding : ITenantScoped
{
    public const int MaxNotesLength = 4000;

    private Finding() { }

    public Finding(Guid workOrderId, string technician, ConditionLabel condition, string notes, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        WorkOrderId = workOrderId;
        Technician = technician;
        Condition = condition;
        Notes = notes;
        RecordedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid WorkOrderId { get; private set; }
    public string Technician { get; private set; } = string.Empty;
    public ConditionLabel Condition { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public List<Attachment> Attachments { get; private set; } = [];
}

/// <summary>
/// Metadata of an uploaded file. The bytes live in the attachment store under <see cref="StorageKey"/>, a generated
/// name: the user's file name is kept only for display and never used as a path.
/// </summary>
internal sealed class Attachment : ITenantScoped
{
    private Attachment() { }

    public Attachment(Guid findingId, string fileName, string contentType, long sizeBytes, string sha256, string storageKey, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        FindingId = findingId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        StorageKey = storageKey;
        UploadedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid FindingId { get; private set; }
    public string FileName { get; private set; } = string.Empty;

    /// <summary>The type detected from the file's magic bytes, not the one the client declared.</summary>
    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }
}
