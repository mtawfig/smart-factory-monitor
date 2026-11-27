using System.Security.Claims;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Modules.Maintenance.Domain;

namespace SmartFactory.Modules.Maintenance.Endpoints;

// ── Requests ───────────────────────────────────────────────────────────────────────────────────────────────
internal sealed record CreateMaintenanceRequest(Guid AssetId, string Title, string Reason, string Priority, IReadOnlyList<string>? Checklist, DateOnly? DueDate);

internal sealed record EditWorkOrderRequest(uint Version, string Title, string? Description, string Priority, DateOnly? DueDate, IReadOnlyList<string> Checklist);

internal sealed record VersionRequest(uint Version);

internal sealed record ReasonRequest(uint Version, string? Reason);

internal sealed record ScheduleRequest(uint Version, string Assignee, DateOnly ScheduledFor);

internal sealed record CloseRequest(uint Version, string? Disposition);

internal sealed record ChecklistItemUpdate(uint Version, string Status, string? Note);

internal sealed record CreateFindingRequest(string Condition, string Notes);

// ── Responses ──────────────────────────────────────────────────────────────────────────────────────────────
internal sealed record WorkOrderSummaryDto(
    Guid Id, Guid AssetId, string AssetCode, string Title, string Priority, string Status, string? AssignedTo,
    DateOnly? ScheduledFor, DateOnly? DueDate, int Revision, DateTimeOffset UpdatedAtUtc);

internal sealed record ChecklistItemDto(Guid Id, int Position, string Text, string Status, string? Note);

internal sealed record HistoryEntryDto(int Revision, string Action, string FromStatus, string ToStatus, string Actor, string? Note, DateTimeOffset AtUtc);

internal sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset UploadedAtUtc);

internal sealed record FindingDto(Guid Id, string Technician, string Condition, string Notes, DateTimeOffset RecordedAtUtc, IReadOnlyList<AttachmentDto> Attachments);

internal sealed record RequestInfoDto(Guid Id, string Reason, string RequestedBy, DateTimeOffset RequestedAtUtc, string Decision);

internal sealed record WorkOrderDetailDto(
    Guid Id, Guid AssetId, string AssetCode, string Title, string? Description, string Priority, string Status,
    string? StatusReason, string? AssignedTo, DateOnly? ScheduledFor, DateOnly? DueDate, string? ApprovedBy,
    string? Disposition, int Revision, uint Version, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    RequestInfoDto Request, IReadOnlyList<ChecklistItemDto> Checklist, IReadOnlyList<HistoryEntryDto> History,
    IReadOnlyList<FindingDto> Findings, IReadOnlyList<string> AvailableActions);

/// <summary>What the audit trail stores before and after each change: the fields a reviewer cares about.</summary>
internal sealed record WorkOrderSnapshot(
    string Status, int Revision, string Title, string Priority, string? AssignedTo, DateOnly? ScheduledFor,
    DateOnly? DueDate, string? Disposition, string? StatusReason, IReadOnlyList<string> Checklist)
{
    public static WorkOrderSnapshot Of(WorkOrder w) => new(
        w.Status.ToString(), w.Revision, w.Title, w.Priority.ToString(), w.AssignedTo, w.ScheduledFor, w.DueDate,
        w.Disposition?.ToString(), w.StatusReason, [.. w.Checklist.OrderBy(c => c.Position).Select(c => $"{c.Text}: {c.Status}")]);
}

internal static class MaintenanceValidation
{
    /// <summary>True only for an exact enum name; numbers and other casings are refused.</summary>
    public static bool IsName<TEnum>(string? value) where TEnum : struct, Enum =>
        value is not null && System.Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal);

    public static Dictionary<string, string[]> Validate(CreateMaintenanceRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        Text(errors, "title", r.Title, 3, 200);
        Text(errors, "reason", r.Reason, 3, 2000);
        EnumName<Priority>(errors, "priority", r.Priority);
        Checklist(errors, r.Checklist ?? []);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(EditWorkOrderRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        Text(errors, "title", r.Title, 3, 200);
        if (r.Description is { Length: > 2000 })
        {
            errors["description"] = ["At most 2000 characters."];
        }

        EnumName<Priority>(errors, "priority", r.Priority);
        Checklist(errors, r.Checklist ?? []);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(CreateFindingRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        EnumName<ConditionLabel>(errors, "condition", r.Condition);
        Text(errors, "notes", r.Notes, 3, Finding.MaxNotesLength);
        return errors;
    }

    private static void Text(Dictionary<string, string[]> errors, string field, string? value, int min, int max)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < min || length > max)
        {
            errors[field] = [$"Between {min} and {max} characters."];
        }
    }

    private static void EnumName<TEnum>(Dictionary<string, string[]> errors, string field, string? value) where TEnum : struct, Enum
    {
        if (!IsName<TEnum>(value))
        {
            errors[field] = [$"One of: {string.Join(", ", System.Enum.GetNames<TEnum>())}."];
        }
    }

    private static void Checklist(Dictionary<string, string[]> errors, IReadOnlyList<string> items)
    {
        if (items.Count > WorkOrder.MaxChecklistItems)
        {
            errors["checklist"] = [$"At most {WorkOrder.MaxChecklistItems} items."];
        }
        else if (items.Any(i => i is null || i.Trim().Length is 0 or > 300))
        {
            errors["checklist"] = ["Each item needs 1 to 300 characters."];
        }
    }
}

internal static class CallerMapping
{
    /// <summary>Turns the validated token into the domain's view of the caller.</summary>
    public static Caller ToCaller(this ClaimsPrincipal user) => new(
        Username: user.Identity?.Name ?? string.Empty,
        IsApprover: user.IsInRole(Roles.MaintenancePlanner) || user.IsInRole(Roles.ReliabilityManager) || user.IsInRole(Roles.Administrator),
        IsPlanner: user.IsInRole(Roles.MaintenancePlanner) || user.IsInRole(Roles.Administrator),
        IsTechnician: user.IsInRole(Roles.Technician));

    /// <summary>A technician who is not also a planner or approver sees only the work assigned to them.</summary>
    public static bool SeesOnlyOwnWork(this Caller caller) => caller.IsTechnician && !caller.IsPlanner && !caller.IsApprover;
}
