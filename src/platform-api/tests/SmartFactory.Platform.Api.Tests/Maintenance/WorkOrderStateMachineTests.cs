using SmartFactory.Modules.Maintenance.Domain;

namespace SmartFactory.Platform.Api.Tests.Maintenance;

/// <summary>
/// Unit tests for the work-order lifecycle, without a database. The exhaustive test walks every (state, action) pair:
/// adding a state or an action without deciding its transitions makes it fail.
/// </summary>
public sealed class WorkOrderStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private static readonly Caller Planner = new("planner", IsApprover: true, IsPlanner: true, IsTechnician: false);
    private static readonly Caller Manager = new("manager", IsApprover: true, IsPlanner: false, IsTechnician: false);
    private static readonly Caller Tech = new("technician", IsApprover: false, IsPlanner: false, IsTechnician: true);
    private static readonly Caller OtherTech = new("technician-2", IsApprover: false, IsPlanner: false, IsTechnician: true);

    /// <summary>The expected lifecycle, written out independently of the implementation's table.</summary>
    private static readonly Dictionary<(WorkOrderStatus, WorkOrderAction), WorkOrderStatus> Expected = new()
    {
        [(WorkOrderStatus.Requested, WorkOrderAction.Approve)] = WorkOrderStatus.Approved,
        [(WorkOrderStatus.Requested, WorkOrderAction.Reject)] = WorkOrderStatus.Rejected,
        [(WorkOrderStatus.Requested, WorkOrderAction.Cancel)] = WorkOrderStatus.Cancelled,
        [(WorkOrderStatus.Approved, WorkOrderAction.Schedule)] = WorkOrderStatus.Scheduled,
        [(WorkOrderStatus.Approved, WorkOrderAction.Cancel)] = WorkOrderStatus.Cancelled,
        [(WorkOrderStatus.Scheduled, WorkOrderAction.Schedule)] = WorkOrderStatus.Scheduled,
        [(WorkOrderStatus.Scheduled, WorkOrderAction.Start)] = WorkOrderStatus.InProgress,
        [(WorkOrderStatus.Scheduled, WorkOrderAction.Hold)] = WorkOrderStatus.OnHold,
        [(WorkOrderStatus.Scheduled, WorkOrderAction.Cancel)] = WorkOrderStatus.Cancelled,
        [(WorkOrderStatus.InProgress, WorkOrderAction.Hold)] = WorkOrderStatus.OnHold,
        [(WorkOrderStatus.InProgress, WorkOrderAction.Complete)] = WorkOrderStatus.Completed,
        [(WorkOrderStatus.InProgress, WorkOrderAction.Cancel)] = WorkOrderStatus.Cancelled,
        [(WorkOrderStatus.OnHold, WorkOrderAction.Resume)] = WorkOrderStatus.Scheduled,
        [(WorkOrderStatus.OnHold, WorkOrderAction.Cancel)] = WorkOrderStatus.Cancelled,
        [(WorkOrderStatus.Completed, WorkOrderAction.Close)] = WorkOrderStatus.Closed,
    };

    // Why strings: the domain enums are internal to the module, and a public test method cannot take them as parameters.
    public static TheoryData<string, string> AllPairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var status in Enum.GetNames<WorkOrderStatus>())
        {
            foreach (var action in Enum.GetNames<WorkOrderAction>())
            {
                data.Add(status, action);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Every_state_and_action_pair_is_either_allowed_with_the_expected_target_or_refused(string fromName, string actionName)
    {
        var from = Enum.Parse<WorkOrderStatus>(fromName);
        var action = Enum.Parse<WorkOrderAction>(actionName);
        var workOrder = InState(from);

        var act = () => Apply(workOrder, action);

        if (Expected.TryGetValue((from, action), out var to))
        {
            act.Should().NotThrow();
            workOrder.Status.Should().Be(to);
        }
        else
        {
            act.Should().Throw<WorkOrderRuleException>().Which.Code.Should().Be("illegal-transition");
            workOrder.Status.Should().Be(from, "a refused transition changes nothing");
        }
    }

    [Fact]
    public void The_implementation_table_matches_the_expected_lifecycle_exactly()
    {
        WorkOrderStateMachine.Transitions.ToDictionary(t => (t.From, t.Action), t => t.To).Should().BeEquivalentTo(Expected);
    }

    [Fact]
    public void Terminal_states_allow_nothing()
    {
        foreach (var terminal in new[] { WorkOrderStatus.Closed, WorkOrderStatus.Rejected, WorkOrderStatus.Cancelled })
        {
            WorkOrderStateMachine.Transitions.Should().NotContain(t => t.From == terminal, $"{terminal} is final");
        }
    }

    [Fact]
    public void The_happy_path_runs_from_request_to_close_and_records_every_revision()
    {
        var (workOrder, request) = NewWorkOrder(["Check bearing play", "Measure vibration"]);

        workOrder.Approve(Manager, request, Now);
        workOrder.Schedule(Planner, "technician", new DateOnly(2026, 11, 3), Now);
        workOrder.Start(Tech, Now);
        ResolveChecklist(workOrder);
        workOrder.Complete(Tech, Now);
        workOrder.Close(Planner, Disposition.Repaired, Now);

        workOrder.Status.Should().Be(WorkOrderStatus.Closed);
        workOrder.Disposition.Should().Be(Disposition.Repaired);
        request.Decision.Should().Be(RequestDecision.Approved);
        workOrder.ApprovedBy.Should().Be("manager");
        workOrder.History.Select(h => h.Action).Should().Equal(
            "Requested", "Approve", "Schedule", "Start", "Checklist", "Checklist", "Complete", "Close");
        workOrder.History.Select(h => h.Revision).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        workOrder.Revision.Should().Be(workOrder.History.Count);
    }

    [Fact]
    public void Only_approvers_approve_and_only_planners_schedule_close_and_cancel()
    {
        var (workOrder, request) = NewWorkOrder([]);

        FailsWith(() => workOrder.Approve(Tech, request, Now), "forbidden");
        workOrder.Approve(Manager, request, Now);
        FailsWith(() => workOrder.Schedule(Manager, "technician", new DateOnly(2026, 11, 3), Now), "forbidden");
        FailsWith(() => workOrder.Cancel(Tech, "no", Now), "forbidden");
        workOrder.Schedule(Planner, "technician", new DateOnly(2026, 11, 3), Now);
        workOrder.Status.Should().Be(WorkOrderStatus.Scheduled);
    }

    [Fact]
    public void Only_the_assigned_technician_starts_completes_and_records_the_work()
    {
        var workOrder = InState(WorkOrderStatus.Scheduled);

        FailsWith(() => workOrder.Start(OtherTech, Now), "not-assigned");
        FailsWith(() => workOrder.Start(Planner, Now), "forbidden");
        workOrder.Start(Tech, Now);
        FailsWith(() => workOrder.RecordChecklistItem(OtherTech, workOrder.Checklist[0].Id, ChecklistItemStatus.Done, null, Now), "not-assigned");
        FailsWith(() => workOrder.EnsureCanRecordFinding(OtherTech), "not-assigned");
        workOrder.Invoking(w => w.EnsureCanRecordFinding(Tech)).Should().NotThrow();
    }

    [Fact]
    public void Completing_requires_every_checklist_item_resolved()
    {
        var workOrder = InState(WorkOrderStatus.InProgress, checklist: ["One", "Two"]);
        workOrder.RecordChecklistItem(Tech, workOrder.Checklist[0].Id, ChecklistItemStatus.Done, null, Now);

        FailsWith(() => workOrder.Complete(Tech, Now), "checklist-incomplete");

        workOrder.RecordChecklistItem(Tech, workOrder.Checklist[1].Id, ChecklistItemStatus.NotApplicable, "no guard fitted", Now);
        workOrder.Complete(Tech, Now);
        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public void Closing_requires_a_disposition()
    {
        var workOrder = InState(WorkOrderStatus.Completed);

        FailsWith(() => workOrder.Close(Planner, null, Now), "disposition-required");
        workOrder.Close(Planner, Disposition.NoFaultFound, Now);
        workOrder.Disposition.Should().Be(Disposition.NoFaultFound);
    }

    [Theory]
    [InlineData(nameof(WorkOrderAction.Reject))]
    [InlineData(nameof(WorkOrderAction.Hold))]
    [InlineData(nameof(WorkOrderAction.Cancel))]
    public void Reject_hold_and_cancel_require_a_reason(string actionName)
    {
        var action = Enum.Parse<WorkOrderAction>(actionName);
        var (workOrder, request) = NewWorkOrder([]);
        if (action == WorkOrderAction.Hold)
        {
            workOrder = InState(WorkOrderStatus.Scheduled);
        }

        Action act = action switch
        {
            WorkOrderAction.Reject => () => workOrder.Reject(Planner, request, "  ", Now),
            WorkOrderAction.Hold => () => workOrder.Hold(Planner, "", Now),
            _ => () => workOrder.Cancel(Planner, " ", Now),
        };

        act.Should().Throw<WorkOrderRuleException>().Which.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(nameof(WorkOrderStatus.Scheduled))]
    [InlineData(nameof(WorkOrderStatus.InProgress))]
    public void Resume_returns_to_the_state_the_work_order_was_held_from(string heldFromName)
    {
        var heldFrom = Enum.Parse<WorkOrderStatus>(heldFromName);
        var workOrder = InState(heldFrom);

        workOrder.Hold(Tech, "Waiting for a part", Now);
        workOrder.StatusReason.Should().Be("Waiting for a part");
        workOrder.Resume(Planner, Now);

        workOrder.Status.Should().Be(heldFrom);
        workOrder.StatusReason.Should().BeNull();
    }

    [Fact]
    public void Edits_are_allowed_before_work_starts_and_keep_the_status_of_unchanged_checklist_lines()
    {
        var workOrder = InState(WorkOrderStatus.Scheduled, checklist: ["Keep me", "Drop me"]);
        var keptId = workOrder.Checklist[0].Id;
        var revision = workOrder.Revision;

        workOrder.Edit(Planner, "New title", null, Priority.Urgent, new DateOnly(2026, 11, 9), ["New first", "Keep me"], Now);

        workOrder.Title.Should().Be("New title");
        workOrder.Checklist.Select(c => (c.Position, c.Text)).Should().Equal((1, "New first"), (2, "Keep me"));
        workOrder.Checklist[1].Id.Should().Be(keptId);
        workOrder.RemovedChecklist.Should().ContainSingle(c => c.Text == "Drop me");
        workOrder.Revision.Should().Be(revision + 1);

        workOrder.Start(Tech, Now);
        FailsWith(() => workOrder.Edit(Planner, "Too late", null, Priority.Low, null, [], Now), "not-editable");
        FailsWith(() => InState(WorkOrderStatus.Requested).Edit(Tech, "x", null, Priority.Low, null, [], Now), "forbidden");
    }

    [Fact]
    public void Available_actions_depend_on_state_role_and_assignment()
    {
        var scheduled = InState(WorkOrderStatus.Scheduled);

        scheduled.AvailableActions(Tech).Should().BeEquivalentTo([WorkOrderAction.Start, WorkOrderAction.Hold]);
        scheduled.AvailableActions(OtherTech).Should().BeEmpty();
        scheduled.AvailableActions(Planner).Should().BeEquivalentTo([WorkOrderAction.Schedule, WorkOrderAction.Hold, WorkOrderAction.Cancel]);
        scheduled.AvailableActions(Manager).Should().BeEmpty("a reliability manager approves requests but does not run the work");
    }

    private static (WorkOrder WorkOrder, MaintenanceRequest Request) NewWorkOrder(IReadOnlyList<string> checklist)
    {
        var request = new MaintenanceRequest(Guid.CreateVersion7(), "Bearing noise on MTR-042", "Vibration above alarm band", Priority.High, "planner", Now);
        return (new WorkOrder(request, "MTR-042", checklist, null, "planner", Now), request);
    }

    /// <summary>Drives a fresh work order into the given state through legal transitions only.</summary>
    private static WorkOrder InState(WorkOrderStatus status, IReadOnlyList<string>? checklist = null)
    {
        var (w, r) = NewWorkOrder(checklist ?? ["Inspect"]);
        if (status == WorkOrderStatus.Rejected)
        {
            w.Reject(Planner, r, "Duplicate", Now);
        }
        else if (status == WorkOrderStatus.Cancelled)
        {
            w.Cancel(Planner, "Not needed", Now);
        }
        else if (status != WorkOrderStatus.Requested)
        {
            Advance(w, r, status);
        }

        w.Status.Should().Be(status);
        return w;
    }

    private static void Advance(WorkOrder w, MaintenanceRequest r, WorkOrderStatus target)
    {
        w.Approve(Planner, r, Now);
        if (target == WorkOrderStatus.Approved)
        {
            return;
        }

        w.Schedule(Planner, "technician", new DateOnly(2026, 11, 3), Now);
        if (target == WorkOrderStatus.Scheduled)
        {
            return;
        }

        if (target == WorkOrderStatus.OnHold)
        {
            w.Hold(Planner, "Waiting for parts", Now);
            return;
        }

        w.Start(Tech, Now);
        if (target == WorkOrderStatus.InProgress)
        {
            return;
        }

        ResolveChecklist(w);
        w.Complete(Tech, Now);
        if (target == WorkOrderStatus.Completed)
        {
            return;
        }

        w.Close(Planner, Disposition.Repaired, Now);
    }

    private static void ResolveChecklist(WorkOrder w)
    {
        foreach (var item in w.Checklist)
        {
            w.RecordChecklistItem(Tech, item.Id, ChecklistItemStatus.Done, null, Now);
        }
    }

    /// <summary>Performs an action with valid inputs and the right actor, so only the state decides the outcome.</summary>
    private static void Apply(WorkOrder w, WorkOrderAction action)
    {
        var request = new MaintenanceRequest(w.AssetId, "x", "x", Priority.Low, "planner", Now);
        if (w.Status == WorkOrderStatus.InProgress && action == WorkOrderAction.Complete)
        {
            ResolveChecklist(w);
        }

        switch (action)
        {
            case WorkOrderAction.Approve: w.Approve(Planner, request, Now); break;
            case WorkOrderAction.Reject: w.Reject(Planner, request, "reason", Now); break;
            case WorkOrderAction.Schedule: w.Schedule(Planner, "technician", new DateOnly(2026, 11, 4), Now); break;
            case WorkOrderAction.Start: w.Start(Tech, Now); break;
            case WorkOrderAction.Hold: w.Hold(Planner, "reason", Now); break;
            case WorkOrderAction.Resume: w.Resume(Planner, Now); break;
            case WorkOrderAction.Complete: w.Complete(Tech, Now); break;
            case WorkOrderAction.Close: w.Close(Planner, Disposition.Repaired, Now); break;
            case WorkOrderAction.Cancel: w.Cancel(Planner, "reason", Now); break;
            default: throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private static void FailsWith(Action act, string code) =>
        act.Should().Throw<WorkOrderRuleException>().Which.Code.Should().Be(code);
}
