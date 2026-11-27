using System.Net;
using System.Net.Http.Json;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests.Maintenance;

/// <summary>The Maintenance API end to end on PostgreSQL: lifecycle, roles, assignment, concurrency, uploads, audit.</summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class MaintenanceApiTests(PlatformApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_request_goes_from_raised_to_closed_with_findings_history_and_audit()
    {
        factory.RequireDatabase();
        var planner = factory.Planner();
        var tech = factory.Technician();

        var w = await factory.RaiseAsync(Ct);
        w.Status.Should().Be("Requested");
        w.AssetCode.Should().Be("MTR-042");
        w.Request.Decision.Should().Be("Pending");
        w.AvailableActions.Should().BeEquivalentTo(["Approve", "Reject", "Cancel"]);

        w = await factory.Manager().ActAsync(w.Id, "approve", new { version = w.Version }, Ct);
        w = await planner.ActAsync(w.Id, "schedule", new { version = w.Version, assignee = Crew.Tech1, scheduledFor = "2026-11-04" }, Ct);
        (await tech.GetFromJsonAsync<WorkOrderDetailModel>($"/api/maintenance/work-orders/{w.Id}", Ct))!
            .AvailableActions.Should().BeEquivalentTo(["Start", "Hold"]);
        w = await tech.ActAsync(w.Id, "start", new { version = w.Version }, Ct);

        foreach (var item in w.Checklist)
        {
            var put = await tech.PutAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/checklist/{item.Id}",
                new { version = w.Version, status = "Done", note = "ok" }, Ct);
            put.StatusCode.Should().Be(HttpStatusCode.OK);
            w = (await put.Content.ReadFromJsonAsync<WorkOrderDetailModel>(Ct))!;
        }

        var findingResponse = await tech.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/findings",
            new { condition = "Worn", notes = "Bearing play confirmed, replaced (synthetic)." }, Ct);
        findingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var finding = (await findingResponse.Content.ReadFromJsonAsync<FindingModel>(Ct))!;
        var upload = await tech.PostAsync($"/api/maintenance/work-orders/{w.Id}/findings/{finding.Id}/attachments",
            Crew.FilePart(Crew.Png, "bearing.png", "image/png"), Ct);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync(Ct));

        w = (await tech.GetFromJsonAsync<WorkOrderDetailModel>($"/api/maintenance/work-orders/{w.Id}", Ct))!;
        w = await tech.ActAsync(w.Id, "complete", new { version = w.Version }, Ct);
        w = await planner.ActAsync(w.Id, "close", new { version = w.Version, disposition = "Repaired" }, Ct);

        w.Status.Should().Be("Closed");
        w.Disposition.Should().Be("Repaired");
        w.ApprovedBy.Should().Be("manager");
        w.Request.Decision.Should().Be("Approved");
        w.Findings.Should().ContainSingle().Which.Attachments.Should().ContainSingle(a => a.ContentType == "image/png" && a.FileName == "bearing.png");
        w.History.Select(h => h.Action).Should().Equal(
            "Requested", "Approve", "Schedule", "Start", "Checklist", "Checklist", "Finding", "Complete", "Close");
        w.History.Select(h => h.Revision).Should().Equal(Enumerable.Range(1, 9));
        w.Revision.Should().Be(9);
        w.AvailableActions.Should().BeEmpty();

        var audit = await factory.CreateClient().As(TestUsers.TenantA, Roles.Administrator)
            .GetFromJsonAsync<List<AuditEventModel>>($"/api/audit/events?aggregateId={w.Id}&take=50", Ct);
        audit!.Select(a => a.Action).Should().BeEquivalentTo(
            ["Created", "Approve", "Schedule", "Start", "Checklist", "Checklist", "Finding", "Complete", "Close"]);
        audit.Should().OnlyContain(a => a.AggregateType == "WorkOrder");
        audit.Single(a => a.Action == "Close").ActorName.Should().Be("planner");
    }

    [Fact]
    public async Task Technicians_see_and_act_on_their_own_work_only()
    {
        factory.RequireDatabase();
        var w = await factory.ScheduledAsync(Ct, technician: Crew.Tech1);
        var other = factory.Technician(Crew.Tech2);

        var get = await other.GetAsync($"/api/maintenance/work-orders/{w.Id}", Ct);
        var start = await other.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/start", new { version = w.Version }, Ct);
        var list = await other.GetFromJsonAsync<List<WorkOrderSummaryModel>>("/api/maintenance/work-orders", Ct);
        var mine = await factory.Technician(Crew.Tech1).GetFromJsonAsync<List<WorkOrderSummaryModel>>("/api/maintenance/work-orders", Ct);

        get.StatusCode.Should().Be(HttpStatusCode.NotFound, "another technician's work is invisible, not forbidden");
        start.StatusCode.Should().Be(HttpStatusCode.NotFound);
        list.Should().NotContain(x => x.Id == w.Id);
        mine.Should().Contain(x => x.Id == w.Id).And.OnlyContain(x => x.AssignedTo == Crew.Tech1);
    }

    [Fact]
    public async Task Roles_are_enforced_by_route_policy_and_by_the_domain()
    {
        factory.RequireDatabase();
        var w = await factory.RaiseAsync(Ct);

        var techApproves = await factory.Technician().PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/approve", new { version = w.Version }, Ct);
        var mlEngineerLists = await factory.CreateClient().As(TestUsers.TenantA, Roles.MlEngineer).GetAsync("/api/maintenance/work-orders", Ct);
        w = await factory.Manager().ActAsync(w.Id, "approve", new { version = w.Version }, Ct);
        var managerSchedules = await factory.Manager().PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/schedule",
            new { version = w.Version, assignee = Crew.Tech1, scheduledFor = "2026-11-04" }, Ct);
        w = await factory.Planner().ActAsync(w.Id, "schedule", new { version = w.Version, assignee = Crew.Tech1, scheduledFor = "2026-11-04" }, Ct);
        var plannerStarts = await factory.Planner().PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/start", new { version = w.Version }, Ct);

        techApproves.StatusCode.Should().Be(HttpStatusCode.Forbidden, "route policy: technicians do not approve");
        mlEngineerLists.StatusCode.Should().Be(HttpStatusCode.Forbidden, "ML engineers do not take part in maintenance");
        managerSchedules.StatusCode.Should().Be(HttpStatusCode.Forbidden, "route policy: only planners schedule");
        plannerStarts.StatusCode.Should().Be(HttpStatusCode.Forbidden, "domain: only the assigned technician starts work");
    }

    [Fact]
    public async Task Illegal_moves_missing_details_and_stale_versions_are_conflicts()
    {
        factory.RequireDatabase();
        var planner = factory.Planner();
        var w = await factory.RaiseAsync(Ct);

        var closeRequested = await planner.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/close", new { version = w.Version, disposition = "Repaired" }, Ct);
        var rejectWithoutReason = await planner.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/reject", new { version = w.Version, reason = "" }, Ct);
        var approved = await planner.ActAsync(w.Id, "approve", new { version = w.Version }, Ct);
        var staleCancel = await planner.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/cancel", new { version = w.Version, reason = "Duplicate" }, Ct);

        (await closeRequested.Content.ReadFromJsonAsync<ProblemModel>(Ct))!.Title.Should().Be("illegal-transition");
        closeRequested.StatusCode.Should().Be(HttpStatusCode.Conflict);
        rejectWithoutReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        staleCancel.StatusCode.Should().Be(HttpStatusCode.Conflict, "the cancel used the version from before the approval");
        (await staleCancel.Content.ReadFromJsonAsync<ProblemModel>(Ct))!.Title.Should().Be("stale-version");
        approved.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Completing_and_closing_enforce_the_checklist_and_the_disposition()
    {
        factory.RequireDatabase();
        var tech = factory.Technician();
        var w = await factory.ScheduledAsync(Ct);
        w = await tech.ActAsync(w.Id, "start", new { version = w.Version }, Ct);

        var early = await tech.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/complete", new { version = w.Version }, Ct);
        foreach (var item in w.Checklist)
        {
            var put = await tech.PutAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/checklist/{item.Id}", new { version = w.Version, status = "NotApplicable", note = (string?)null }, Ct);
            w = (await put.Content.ReadFromJsonAsync<WorkOrderDetailModel>(Ct))!;
        }

        w = await tech.ActAsync(w.Id, "complete", new { version = w.Version }, Ct);
        var noDisposition = await factory.Planner().PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/close", new { version = w.Version, disposition = (string?)null }, Ct);

        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await early.Content.ReadFromJsonAsync<ProblemModel>(Ct))!.Title.Should().Be("checklist-incomplete");
        noDisposition.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await noDisposition.Content.ReadFromJsonAsync<ProblemModel>(Ct))!.Title.Should().Be("disposition-required");
    }

    [Fact]
    public async Task Planners_edit_the_plan_until_work_starts()
    {
        factory.RequireDatabase();
        var planner = factory.Planner();
        var w = await factory.ScheduledAsync(Ct);

        var edit = await planner.PutAsJsonAsync($"/api/maintenance/work-orders/{w.Id}", new
        {
            version = w.Version, title = "Replace bearing, MTR-042", description = "Kit reserved", priority = "Urgent", dueDate = "2026-11-06",
            checklist = new[] { "Lock out and tag out", "Check bearing play", "Measure vibration" },
        }, Ct);
        var edited = (await edit.Content.ReadFromJsonAsync<WorkOrderDetailModel>(Ct))!;
        var started = await factory.Technician().ActAsync(w.Id, "start", new { version = edited.Version }, Ct);
        var lateEdit = await planner.PutAsJsonAsync($"/api/maintenance/work-orders/{w.Id}", new
        {
            version = started.Version, title = "Too late", description = (string?)null, priority = "Low", dueDate = (string?)null, checklist = Array.Empty<string>(),
        }, Ct);

        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        edited.Priority.Should().Be("Urgent");
        edited.Checklist.Select(c => c.Text).Should().Equal("Lock out and tag out", "Check bearing play", "Measure vibration");
        edited.History[^1].Action.Should().Be("Edited");
        lateEdit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await lateEdit.Content.ReadFromJsonAsync<ProblemModel>(Ct))!.Title.Should().Be("not-editable");
    }

    [Fact]
    public async Task Uploads_are_checked_by_content_and_size_and_served_safely()
    {
        factory.RequireDatabase();
        var tech = factory.Technician();
        var w = await factory.ScheduledAsync(Ct);
        w = await tech.ActAsync(w.Id, "start", new { version = w.Version }, Ct);
        var finding = (await (await tech.PostAsJsonAsync($"/api/maintenance/work-orders/{w.Id}/findings",
            new { condition = "Good", notes = "No visible wear." }, Ct)).Content.ReadFromJsonAsync<FindingModel>(Ct))!;
        var url = $"/api/maintenance/work-orders/{w.Id}/findings/{finding.Id}/attachments";

        // An executable ("MZ" header) dressed up as a PDF: name and declared type say PDF, the bytes say otherwise.
        var disguised = await tech.PostAsync(url, Crew.FilePart([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00], "invoice.pdf", "application/pdf"), Ct);
        var tooLarge = await tech.PostAsync(url, Crew.FilePart([.. Crew.Png, .. new byte[5 * 1024 * 1024]], "big.png", "image/png"), Ct);
        // A real PDF header with a hostile name: accepted, but the name is reduced to a safe display name.
        var pdf = await tech.PostAsync(url, Crew.FilePart("%PDF-1.7\n%synthetic\n"u8.ToArray(), "../../etc/passwd.pdf", "text/html"), Ct);
        var saved = (await pdf.Content.ReadFromJsonAsync<AttachmentModel>(Ct))!;
        var download = await tech.GetAsync($"/api/maintenance/attachments/{saved.Id}", Ct);

        disguised.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        tooLarge.StatusCode.Should().BeOneOf(HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.BadRequest);
        pdf.StatusCode.Should().Be(HttpStatusCode.Created);
        saved.ContentType.Should().Be("application/pdf", "the type comes from the bytes, not the declared text/html");
        saved.FileName.Should().Be("passwd.pdf");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        download.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        download.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        (await download.Content.ReadAsByteArrayAsync(Ct)).Should().StartWith("%PDF-"u8.ToArray());
        Directory.EnumerateFiles(factory.AttachmentRoot, "*", SearchOption.AllDirectories)
            .Should().OnlyContain(path => !path.Contains("passwd", StringComparison.Ordinal), "stored under a generated key");
    }

    [Fact]
    public async Task Requests_are_validated_and_need_an_asset_of_the_callers_tenant()
    {
        factory.RequireDatabase();
        var planner = factory.Planner();

        var invalid = await planner.PostAsJsonAsync("/api/maintenance/requests", new { assetId = Guid.NewGuid(), title = "x", reason = "", priority = "Critical" }, Ct);
        var unknownAsset = await planner.PostAsJsonAsync("/api/maintenance/requests",
            new { assetId = Guid.NewGuid(), title = "Valid title", reason = "Valid reason", priority = "Low" }, Ct);

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await invalid.Content.ReadAsStringAsync(Ct);
        problem.Should().Contain("title").And.Contain("reason").And.Contain("priority");
        unknownAsset.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
