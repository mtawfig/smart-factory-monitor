namespace SmartFactory.Modules.Maintenance.Domain;

/// <summary>Lifecycle of a work order (blueprint: work-order state machine).</summary>
internal enum WorkOrderStatus
{
    Requested,
    Approved,
    Scheduled,
    InProgress,
    Completed,
    Closed,
    Rejected,
    OnHold,
    Cancelled,
}

/// <summary>Everything a person can do to a work order's state. Edits that do not change state are not actions.</summary>
internal enum WorkOrderAction
{
    Approve,
    Reject,
    Schedule,
    Start,
    Hold,
    Resume,
    Complete,
    Close,
    Cancel,
}

/// <summary>Who may perform an action. Resolved against the caller's roles and, for technicians, the assignment.</summary>
internal enum Actor
{
    /// <summary>Planner, reliability manager or administrator: may approve or reject requests.</summary>
    Approver,

    /// <summary>Planner or administrator: plans, schedules, holds, cancels and closes.</summary>
    Planner,

    /// <summary>The technician assigned to this work order, and nobody else.</summary>
    AssignedTechnician,
}

/// <summary>
/// The transition table: every legal move, who may make it, and nothing else. Anything not listed is refused.
/// Why a table: the whole lifecycle is reviewable in one screen, and a test can enumerate it exhaustively.
/// </summary>
internal static class WorkOrderStateMachine
{
    internal sealed record Transition(WorkOrderStatus From, WorkOrderAction Action, WorkOrderStatus To, IReadOnlySet<Actor> AllowedActors);

    private static readonly IReadOnlySet<Actor> Approvers = new HashSet<Actor> { Actor.Approver };
    private static readonly IReadOnlySet<Actor> Planners = new HashSet<Actor> { Actor.Planner };
    private static readonly IReadOnlySet<Actor> Technician = new HashSet<Actor> { Actor.AssignedTechnician };
    private static readonly IReadOnlySet<Actor> PlannerOrTechnician = new HashSet<Actor> { Actor.Planner, Actor.AssignedTechnician };

    /// <summary>Open states: not yet finished one way or another. Cancel is allowed from all of them.</summary>
    public static readonly IReadOnlySet<WorkOrderStatus> Open = new HashSet<WorkOrderStatus>
    {
        WorkOrderStatus.Requested, WorkOrderStatus.Approved, WorkOrderStatus.Scheduled, WorkOrderStatus.InProgress, WorkOrderStatus.OnHold,
    };

    /// <summary>States in which the plan (title, description, checklist, due date) may still be edited.</summary>
    public static readonly IReadOnlySet<WorkOrderStatus> Editable = new HashSet<WorkOrderStatus>
    {
        WorkOrderStatus.Requested, WorkOrderStatus.Approved, WorkOrderStatus.Scheduled,
    };

    public static readonly IReadOnlyList<Transition> Transitions =
    [
        new(WorkOrderStatus.Requested, WorkOrderAction.Approve, WorkOrderStatus.Approved, Approvers),
        new(WorkOrderStatus.Requested, WorkOrderAction.Reject, WorkOrderStatus.Rejected, Approvers),
        new(WorkOrderStatus.Approved, WorkOrderAction.Schedule, WorkOrderStatus.Scheduled, Planners),
        // Re-scheduling (new technician or date) while scheduled is the same action, not a new state.
        new(WorkOrderStatus.Scheduled, WorkOrderAction.Schedule, WorkOrderStatus.Scheduled, Planners),
        new(WorkOrderStatus.Scheduled, WorkOrderAction.Start, WorkOrderStatus.InProgress, Technician),
        new(WorkOrderStatus.Scheduled, WorkOrderAction.Hold, WorkOrderStatus.OnHold, PlannerOrTechnician),
        new(WorkOrderStatus.InProgress, WorkOrderAction.Hold, WorkOrderStatus.OnHold, PlannerOrTechnician),
        // Resume returns to the state the work order was held from (Scheduled or In progress). The table shows the
        // usual target; WorkOrder.Resume uses the recorded HeldFrom state.
        new(WorkOrderStatus.OnHold, WorkOrderAction.Resume, WorkOrderStatus.Scheduled, PlannerOrTechnician),
        new(WorkOrderStatus.InProgress, WorkOrderAction.Complete, WorkOrderStatus.Completed, Technician),
        new(WorkOrderStatus.Completed, WorkOrderAction.Close, WorkOrderStatus.Closed, Planners),
        .. Open.Select(from => new Transition(from, WorkOrderAction.Cancel, WorkOrderStatus.Cancelled, Planners)),
    ];

    public static Transition? Find(WorkOrderStatus from, WorkOrderAction action) =>
        Transitions.FirstOrDefault(t => t.From == from && t.Action == action);

    /// <summary>Actions the given caller could take right now; drives which buttons the UI shows.</summary>
    public static IReadOnlyList<WorkOrderAction> AvailableActions(WorkOrderStatus from, IReadOnlySet<Actor> caller) =>
        [.. Transitions.Where(t => t.From == from && t.AllowedActors.Overlaps(caller)).Select(t => t.Action).Distinct()];
}
