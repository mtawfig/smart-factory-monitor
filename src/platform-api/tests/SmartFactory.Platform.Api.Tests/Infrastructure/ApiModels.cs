namespace SmartFactory.Platform.Api.Tests.Infrastructure;

// Client-side views of the API's JSON. Deliberately separate from the server's internal DTOs: the tests consume the
// API the way the Angular app does, through its wire format.

internal sealed record PlantModel(Guid Id, string Code, string Name, string TimeZone, List<LineModel> Lines);

internal sealed record LineModel(Guid Id, string Code, string Name, int AssetCount);

internal sealed record AssetSummaryModel(
    Guid Id, string Code, string Name, string AssetClass, string Criticality, string Status,
    Guid LineId, string LineCode, string LineName, string PlantCode, int SensorCount);

internal sealed record SensorModel(
    Guid Id, string Metric, string Unit, decimal IntervalSeconds, decimal? NominalMin, decimal? NominalMax, string? Kind, string SourceTopic);

internal sealed record AssetDetailModel(
    Guid Id, string Code, string Name, string AssetClass, string Criticality, string Status, string? Description,
    decimal? RatedPowerKw, Guid LineId, string LineCode, string LineName, string PlantCode, uint Version,
    DateTimeOffset UpdatedAtUtc, List<SensorModel> Sensors);

internal sealed record AuditEventModel(
    long Sequence, DateTimeOffset OccurredAtUtc, string ActorId, string ActorName, string AggregateType, string AggregateId,
    string Action, string? BeforeState, string? AfterState, string? BeforeHash, string? AfterHash, string CorrelationId, string ChainHash);

internal sealed record AuditVerificationModel(bool Intact, long EventsChecked, long? FirstBrokenSequence);

internal sealed record WorkOrderSummaryModel(
    Guid Id, Guid AssetId, string AssetCode, string Title, string Priority, string Status, string? AssignedTo,
    DateOnly? ScheduledFor, DateOnly? DueDate, int Revision, DateTimeOffset UpdatedAtUtc);

internal sealed record ChecklistItemModel(Guid Id, int Position, string Text, string Status, string? Note);

internal sealed record HistoryEntryModel(int Revision, string Action, string FromStatus, string ToStatus, string Actor, string? Note, DateTimeOffset AtUtc);

internal sealed record AttachmentModel(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset UploadedAtUtc);

internal sealed record FindingModel(Guid Id, string Technician, string Condition, string Notes, DateTimeOffset RecordedAtUtc, List<AttachmentModel> Attachments);

internal sealed record RequestInfoModel(Guid Id, string Reason, string RequestedBy, DateTimeOffset RequestedAtUtc, string Decision);

internal sealed record WorkOrderDetailModel(
    Guid Id, Guid AssetId, string AssetCode, string Title, string? Description, string Priority, string Status,
    string? StatusReason, string? AssignedTo, DateOnly? ScheduledFor, DateOnly? DueDate, string? ApprovedBy,
    string? Disposition, int Revision, uint Version, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    RequestInfoModel Request, List<ChecklistItemModel> Checklist, List<HistoryEntryModel> History,
    List<FindingModel> Findings, List<string> AvailableActions);

internal sealed record ProblemModel(string? Title, string? Detail, int? Status);
