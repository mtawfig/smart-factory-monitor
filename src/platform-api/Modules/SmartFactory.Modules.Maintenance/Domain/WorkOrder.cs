using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.Modules.Maintenance.Domain;

internal enum Priority { Low, Medium, High, Urgent }

/// <summary>How the work ended (blueprint): it drives the simulator's health reset and the ML outcome labels later.</summary>
internal enum Disposition { Repaired, NoFaultFound, Deferred }

internal enum ChecklistItemStatus { Pending, Done, NotApplicable, Failed }

internal enum RequestDecision { Pending, Approved, Rejected }

/// <summary>A domain rule was broken (illegal transition, missing reason, wrong technician). Maps to HTTP 409 or 403.</summary>
internal sealed class WorkOrderRuleException(string code, string message) : Exception(message)
{
    /// <summary>Stable machine-readable reason, e.g. <c>illegal-transition</c>, <c>not-assigned</c>.</summary>
    public string Code { get; } = code;
}

/// <summary>The caller as the domain sees them: a username and which actor groups they belong to.</summary>
internal sealed record Caller(string Username, bool IsApprover, bool IsPlanner, bool IsTechnician)
{
    /// <summary>Actor groups for this work order: "assigned technician" depends on the work order, not only the role.</summary>
    public IReadOnlySet<Actor> ActorsFor(WorkOrder workOrder)
    {
        var actors = new HashSet<Actor>();
        if (IsApprover)
        {
            actors.Add(Actor.Approver);
        }

        if (IsPlanner)
        {
            actors.Add(Actor.Planner);
        }

        if (IsTechnician && workOrder.AssignedTo is not null && string.Equals(workOrder.AssignedTo, Username, StringComparison.Ordinal))
        {
            actors.Add(Actor.AssignedTechnician);
        }

        return actors;
    }
}

/// <summary>Why someone asked for maintenance. Raised manually now; from an alert in week 8.</summary>
internal sealed class MaintenanceRequest : ITenantScoped
{
    private MaintenanceRequest() { }

    public MaintenanceRequest(Guid assetId, string title, string reason, Priority priority, string requestedBy, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        AssetId = assetId;
        Title = title;
        Reason = reason;
        Priority = priority;
        RequestedBy = requestedBy;
        RequestedAtUtc = now;
        Decision = RequestDecision.Pending;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid AssetId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public Priority Priority { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public RequestDecision Decision { get; private set; }

    internal void Decide(RequestDecision decision) => Decision = decision;
}

/// <summary>One line of the technician's checklist.</summary>
internal sealed class ChecklistItem : ITenantScoped
{
    private ChecklistItem() { }

    public ChecklistItem(Guid workOrderId, int position, string text)
    {
        Id = Guid.CreateVersion7();
        WorkOrderId = workOrderId;
        Position = position;
        Text = text;
        Status = ChecklistItemStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid WorkOrderId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public ChecklistItemStatus Status { get; private set; }
    public string? Note { get; private set; }

    internal void MoveTo(int position) => Position = position;

    internal void Record(ChecklistItemStatus status, string? note)
    {
        Status = status;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}

/// <summary>One entry of the work order's own timeline: every transition and edit, with its revision number.</summary>
internal sealed class WorkOrderHistoryEntry : ITenantScoped
{
    private WorkOrderHistoryEntry() { }

    public WorkOrderHistoryEntry(Guid workOrderId, int revision, string action, WorkOrderStatus from, WorkOrderStatus to, string actor, string? note, DateTimeOffset at)
    {
        Id = Guid.CreateVersion7();
        WorkOrderId = workOrderId;
        Revision = revision;
        Action = action;
        FromStatus = from;
        ToStatus = to;
        Actor = actor;
        Note = note;
        AtUtc = at;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid WorkOrderId { get; private set; }
    public int Revision { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public WorkOrderStatus FromStatus { get; private set; }
    public WorkOrderStatus ToStatus { get; private set; }
    public string Actor { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public DateTimeOffset AtUtc { get; private set; }
}

/// <summary>
/// The aggregate root. Every change goes through a method here, which checks the transition table and the
/// invariants, raises <see cref="Revision"/> and appends a history entry. Nothing outside sets state directly.
/// </summary>
internal sealed class WorkOrder : ITenantScoped
{
    public const int MaxChecklistItems = 30;

    private WorkOrder() { }

    public WorkOrder(MaintenanceRequest request, string assetCode, IReadOnlyList<string> checklist, DateOnly? dueDate, string createdBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(checklist);
        Id = Guid.CreateVersion7();
        RequestId = request.Id;
        AssetId = request.AssetId;
        AssetCode = assetCode;
        Title = request.Title;
        Description = request.Reason;
        Priority = request.Priority;
        DueDate = dueDate;
        Status = WorkOrderStatus.Requested;
        CreatedAtUtc = now;
        ReplaceChecklist(checklist);
        Record("Requested", WorkOrderStatus.Requested, WorkOrderStatus.Requested, createdBy, request.Reason, now);
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid RequestId { get; private set; }
    public Guid AssetId { get; private set; }

    /// <summary>Snapshot of the asset code at creation, so lists need no call to the Assets module.</summary>
    public string AssetCode { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public WorkOrderStatus Status { get; private set; }

    /// <summary>State to return to when resuming from On hold.</summary>
    public WorkOrderStatus? HeldFrom { get; private set; }

    public string? AssignedTo { get; private set; }
    public DateOnly? ScheduledFor { get; private set; }
    public string? ApprovedBy { get; private set; }
    public Disposition? Disposition { get; private set; }

    /// <summary>Reason given for the last Reject, Hold or Cancel.</summary>
    public string? StatusReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Business revision: +1 on every transition or edit. Shown to users and recorded in the history.</summary>
    public int Revision { get; private set; }

    /// <summary>PostgreSQL <c>xmin</c>, for optimistic concurrency (409 on a stale write).</summary>
    public uint Version { get; private set; }

    public List<ChecklistItem> Checklist { get; private set; } = [];
    public List<WorkOrderHistoryEntry> History { get; private set; } = [];

    /// <summary>History entries created by the last change, so the caller can add them to the DbContext explicitly.</summary>
    internal List<WorkOrderHistoryEntry> PendingHistory { get; } = [];

    /// <summary>Checklist items created by the last change.</summary>
    internal List<ChecklistItem> PendingChecklist { get; } = [];

    /// <summary>Checklist items removed by the last change.</summary>
    internal List<ChecklistItem> RemovedChecklist { get; } = [];

    public bool ChecklistResolved => Checklist.All(i => i.Status != ChecklistItemStatus.Pending);

    public IReadOnlyList<WorkOrderAction> AvailableActions(Caller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return WorkOrderStateMachine.AvailableActions(Status, caller.ActorsFor(this));
    }

    public void Approve(Caller caller, MaintenanceRequest request, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        Move(WorkOrderAction.Approve, caller, note: null, now);
        ApprovedBy = caller.Username;
        request.Decide(RequestDecision.Approved);
    }

    public void Reject(Caller caller, MaintenanceRequest request, string reason, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transition = Authorize(WorkOrderAction.Reject, caller);
        var why = Required(reason, "A reason is required to reject a request.");
        Apply(transition, caller, why, now);
        StatusReason = why;
        request.Decide(RequestDecision.Rejected);
    }

    public void Schedule(Caller caller, string assignee, DateOnly scheduledFor, DateTimeOffset now)
    {
        var transition = Authorize(WorkOrderAction.Schedule, caller);
        var who = Required(assignee, "A technician must be assigned when scheduling.");
        Apply(transition, caller, $"Assigned to {who} for {scheduledFor:yyyy-MM-dd}", now);
        AssignedTo = who;
        ScheduledFor = scheduledFor;
    }

    public void Start(Caller caller, DateTimeOffset now) => Move(WorkOrderAction.Start, caller, note: null, now);

    public void Hold(Caller caller, string reason, DateTimeOffset now)
    {
        var transition = Authorize(WorkOrderAction.Hold, caller);
        var why = Required(reason, "A reason is required to put a work order on hold.");
        var from = Status;
        Apply(transition, caller, why, now);
        HeldFrom = from;
        StatusReason = why;
    }

    public void Resume(Caller caller, DateTimeOffset now)
    {
        var target = HeldFrom ?? WorkOrderStatus.Scheduled;
        Move(WorkOrderAction.Resume, caller, note: null, now, overrideTarget: target);
        HeldFrom = null;
        StatusReason = null;
    }

    public void Complete(Caller caller, DateTimeOffset now)
    {
        // Why authorise first: an illegal move must report "illegal transition", not a missing detail.
        var transition = Authorize(WorkOrderAction.Complete, caller);
        if (!ChecklistResolved)
        {
            throw new WorkOrderRuleException("checklist-incomplete", "Every checklist item must be done, not applicable or failed before completing.");
        }

        Apply(transition, caller, note: null, now);
    }

    public void Close(Caller caller, Disposition? disposition, DateTimeOffset now)
    {
        var transition = Authorize(WorkOrderAction.Close, caller);
        if (disposition is null)
        {
            throw new WorkOrderRuleException("disposition-required", "Closing requires a disposition: Repaired, NoFaultFound or Deferred.");
        }

        if (!ChecklistResolved)
        {
            throw new WorkOrderRuleException("checklist-incomplete", "Closing requires every checklist item to have a status.");
        }

        Apply(transition, caller, $"Disposition: {disposition}", now);
        Disposition = disposition;
    }

    public void Cancel(Caller caller, string reason, DateTimeOffset now)
    {
        var transition = Authorize(WorkOrderAction.Cancel, caller);
        var why = Required(reason, "A reason is required to cancel a work order.");
        Apply(transition, caller, why, now);
        StatusReason = why;
    }

    /// <summary>Edit the plan. Allowed only before work starts, and only for planners.</summary>
    public void Edit(Caller caller, string title, string? description, Priority priority, DateOnly? dueDate, IReadOnlyList<string> checklist, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(checklist);
        if (!caller.IsPlanner)
        {
            throw new WorkOrderRuleException("forbidden", "Only a planner or administrator may edit a work order.");
        }

        if (!WorkOrderStateMachine.Editable.Contains(Status))
        {
            throw new WorkOrderRuleException("not-editable", $"A work order in state {Status} can no longer be edited.");
        }

        Title = Required(title, "A title is required.");
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        DueDate = dueDate;
        ReplaceChecklist(checklist);
        Record("Edited", Status, Status, caller.Username, note: null, now);
    }

    /// <summary>The assigned technician records the outcome of one checklist line while the work is in progress.</summary>
    public ChecklistItem RecordChecklistItem(Caller caller, Guid itemId, ChecklistItemStatus status, string? note, DateTimeOffset now)
    {
        RequireAssignedTechnicianInProgress(caller);
        var item = Checklist.FirstOrDefault(i => i.Id == itemId)
                   ?? throw new WorkOrderRuleException("not-found", "Checklist item not found.");
        item.Record(status, note);
        Record("Checklist", Status, Status, caller.Username, $"{item.Text}: {status}", now);
        return item;
    }

    /// <summary>Only the assigned technician, only while the work is in progress, may add findings.</summary>
    public void EnsureCanRecordFinding(Caller caller) => RequireAssignedTechnicianInProgress(caller);

    public void NoteFinding(Caller caller, string conditionLabel, DateTimeOffset now) =>
        Record("Finding", Status, Status, caller.Username, $"Condition: {conditionLabel}", now);

    private void RequireAssignedTechnicianInProgress(Caller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (!caller.ActorsFor(this).Contains(Actor.AssignedTechnician))
        {
            throw new WorkOrderRuleException("not-assigned", "Only the technician assigned to this work order may do this.");
        }

        if (Status != WorkOrderStatus.InProgress)
        {
            throw new WorkOrderRuleException("not-in-progress", "This is only possible while the work order is in progress.");
        }
    }

    private void Move(WorkOrderAction action, Caller caller, string? note, DateTimeOffset now, WorkOrderStatus? overrideTarget = null) =>
        Apply(Authorize(action, caller), caller, note, now, overrideTarget);

    /// <summary>Is this move in the table for the current state, and may this caller make it?</summary>
    private WorkOrderStateMachine.Transition Authorize(WorkOrderAction action, Caller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        var transition = WorkOrderStateMachine.Find(Status, action)
                         ?? throw new WorkOrderRuleException("illegal-transition", $"Cannot {action} a work order that is {Status}.");
        if (!transition.AllowedActors.Overlaps(caller.ActorsFor(this)))
        {
            var code = transition.AllowedActors.Contains(Actor.AssignedTechnician) && caller.IsTechnician ? "not-assigned" : "forbidden";
            throw new WorkOrderRuleException(code, $"You are not allowed to {action} this work order.");
        }

        return transition;
    }

    private void Apply(WorkOrderStateMachine.Transition transition, Caller caller, string? note, DateTimeOffset now, WorkOrderStatus? overrideTarget = null)
    {
        var from = Status;
        Status = overrideTarget ?? transition.To;
        Record(transition.Action.ToString(), from, Status, caller.Username, note, now);
    }

    private void Record(string action, WorkOrderStatus from, WorkOrderStatus to, string actor, string? note, DateTimeOffset now)
    {
        Revision++;
        UpdatedAtUtc = now;
        var entry = new WorkOrderHistoryEntry(Id, Revision, action, from, to, actor, note, now);
        History.Add(entry);
        PendingHistory.Add(entry);
    }

    private void ReplaceChecklist(IReadOnlyList<string> lines)
    {
        var texts = lines.Select(l => l?.Trim()).Where(l => !string.IsNullOrEmpty(l)).Cast<string>().ToList();
        if (texts.Count > MaxChecklistItems)
        {
            throw new WorkOrderRuleException("validation", $"A checklist has at most {MaxChecklistItems} items.");
        }

        // Keep items whose text is unchanged (with their status); add new ones; drop the rest.
        var kept = new List<ChecklistItem>();
        for (var i = 0; i < texts.Count; i++)
        {
            var existing = Checklist.FirstOrDefault(c => c.Text == texts[i] && !kept.Contains(c));
            if (existing is not null)
            {
                existing.MoveTo(i + 1);
                kept.Add(existing);
                continue;
            }

            var created = new ChecklistItem(Id, i + 1, texts[i]);
            kept.Add(created);
            PendingChecklist.Add(created);
        }

        RemovedChecklist.AddRange(Checklist.Where(c => !kept.Contains(c)));
        Checklist = kept;
    }

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new WorkOrderRuleException("validation", message) : value.Trim();
}
