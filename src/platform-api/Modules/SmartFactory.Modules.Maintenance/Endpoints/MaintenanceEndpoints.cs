using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SmartFactory.BuildingBlocks.Auditing;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Contracts;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Maintenance.Domain;
using SmartFactory.Modules.Maintenance.Persistence;
using SmartFactory.Modules.Maintenance.Storage;

namespace SmartFactory.Modules.Maintenance.Endpoints;

/// <summary>
/// HTTP surface of the Maintenance module. Route policies decide *which roles* may call an endpoint; the domain decides
/// *whether this caller may act on this work order now* (state, assignment). A technician who is not also a planner
/// sees only their own work orders; anything else answers 404, as does another tenant's id.
/// </summary>
internal static class MaintenanceEndpoints
{
    public const string ParticipantPolicy = "Maintenance.Participant";
    public const string ApproverPolicy = "Maintenance.Approver";
    public const int MaxAttachmentsPerFinding = 5;

    private const string AuditType = "WorkOrder";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(MaintenanceModule.RoutePrefix).WithTags("Maintenance")
            .RequireAuthorization(Policies.TenantMember)
            .RequireAuthorization(ParticipantPolicy);

        group.MapPost("/requests", CreateRequest).WithName("Maintenance.CreateRequest")
            .WithSummary("Raise a maintenance request for an asset; creates a work order in state Requested.");
        group.MapGet("/work-orders", ListWorkOrders).WithName("Maintenance.ListWorkOrders")
            .WithSummary("Work orders, optionally filtered by status or asset. Technicians see only their assigned work.");
        group.MapGet("/work-orders/{id:guid}", GetWorkOrder).WithName("Maintenance.GetWorkOrder")
            .WithSummary("One work order with checklist, history, findings and the actions the caller may take now.");
        group.MapPut("/work-orders/{id:guid}", EditWorkOrder).WithName("Maintenance.EditWorkOrder").WithRuleProblems()
            .WithSummary("Edit the plan before work starts (planner).").RequireAuthorization(Policies.MaintenancePlanner);

        group.MapPost("/work-orders/{id:guid}/approve", Approve).WithName("Maintenance.Approve").WithRuleProblems()
            .WithSummary("Approve a requested work order (planner, reliability manager).").RequireAuthorization(ApproverPolicy);
        group.MapPost("/work-orders/{id:guid}/reject", Reject).WithName("Maintenance.Reject").WithRuleProblems()
            .WithSummary("Reject a requested work order with a reason (planner, reliability manager).").RequireAuthorization(ApproverPolicy);
        group.MapPost("/work-orders/{id:guid}/schedule", Schedule).WithName("Maintenance.Schedule").WithRuleProblems()
            .WithSummary("Assign a technician and a date (planner).").RequireAuthorization(Policies.MaintenancePlanner);
        group.MapPost("/work-orders/{id:guid}/start", Start).WithName("Maintenance.Start").WithRuleProblems()
            .WithSummary("Start the work (assigned technician).").RequireAuthorization(Policies.Technician);
        group.MapPost("/work-orders/{id:guid}/hold", Hold).WithName("Maintenance.Hold").WithRuleProblems()
            .WithSummary("Put scheduled or running work on hold with a reason (planner or assigned technician).");
        group.MapPost("/work-orders/{id:guid}/resume", Resume).WithName("Maintenance.Resume").WithRuleProblems()
            .WithSummary("Resume work from hold (planner or assigned technician).");
        group.MapPost("/work-orders/{id:guid}/complete", Complete).WithName("Maintenance.Complete").WithRuleProblems()
            .WithSummary("Complete the work once every checklist item has a status (assigned technician).").RequireAuthorization(Policies.Technician);
        group.MapPost("/work-orders/{id:guid}/close", Close).WithName("Maintenance.Close").WithRuleProblems()
            .WithSummary("Close completed work with a disposition (planner).").RequireAuthorization(Policies.MaintenancePlanner);
        group.MapPost("/work-orders/{id:guid}/cancel", Cancel).WithName("Maintenance.Cancel").WithRuleProblems()
            .WithSummary("Cancel open work with a reason (planner).").RequireAuthorization(Policies.MaintenancePlanner);

        group.MapPut("/work-orders/{id:guid}/checklist/{itemId:guid}", UpdateChecklistItem).WithName("Maintenance.UpdateChecklistItem").WithRuleProblems()
            .WithSummary("Record the outcome of one checklist item (assigned technician).").RequireAuthorization(Policies.Technician);
        group.MapPost("/work-orders/{id:guid}/findings", CreateFinding).WithName("Maintenance.CreateFinding").WithRuleProblems()
            .WithSummary("Record a finding with a condition label (assigned technician).").RequireAuthorization(Policies.Technician);
        group.MapPost("/work-orders/{id:guid}/findings/{findingId:guid}/attachments", UploadAttachment).WithName("Maintenance.UploadAttachment").WithRuleProblems()
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge).ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .WithSummary("Attach a JPEG, PNG or PDF (max 5 MB) to a finding (assigned technician).")
            .RequireAuthorization(Policies.Technician)
            // Why: bearer tokens are not sent automatically by browsers, so cross-site form posts cannot use them;
            // the anti-forgery token that minimal APIs require for form binding protects cookie auth, which we do not use.
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(FileSignatures.MaxBytes + 64 * 1024));
        group.MapGet("/attachments/{id:guid}", DownloadAttachment).WithName("Maintenance.DownloadAttachment")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/octet-stream", [.. FileSignatures.AllowedContentTypes])
            .WithSummary("Download an attachment. Served as a download with the type detected at upload.");
    }

    /// <summary>Documents the problem responses a domain rule can produce, so the OpenAPI contract lists them.</summary>
    private static RouteHandlerBuilder WithRuleProblems(this RouteHandlerBuilder builder) => builder
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

    // ── Create and read ────────────────────────────────────────────────────────────────────────────────────

    private static async Task<Results<Created<WorkOrderDetailDto>, ValidationProblem, NotFound>> CreateRequest(
        CreateMaintenanceRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow,
        IAssetDirectory assets, TimeProvider clock, CancellationToken ct)
    {
        var errors = MaintenanceValidation.Validate(body);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        // Asset of another tenant, or none at all: both look the same (ADR-008).
        var asset = await assets.FindAsync(body.AssetId, ct);
        if (asset is null)
        {
            return TypedResults.NotFound();
        }

        var caller = http.User.ToCaller();
        var id = await uow.ExecuteAsync(_ =>
        {
            var now = clock.GetUtcNow();
            var request = new MaintenanceRequest(asset.Id, body.Title.Trim(), body.Reason.Trim(), Enum.Parse<Priority>(body.Priority), caller.Username, now);
            var workOrder = new WorkOrder(request, asset.Code, body.Checklist ?? [], body.DueDate, caller.Username, now);
            db.Requests.Add(request);
            db.WorkOrders.Add(workOrder);
            TrackPending(db, workOrder);
            IReadOnlyList<AuditRecord> audit =
            [
                new("MaintenanceRequest", request.Id.ToString(), "Created", null, new { request.AssetId, request.Title, request.Reason, Priority = request.Priority.ToString() }),
                new(AuditType, workOrder.Id.ToString(), "Created", null, WorkOrderSnapshot.Of(workOrder)),
            ];
            return Task.FromResult((workOrder.Id, audit));
        }, ct);

        var detail = (await LoadDetailAsync(db, id, caller, ct))!;
        return TypedResults.Created($"{MaintenanceModule.RoutePrefix}/work-orders/{id}", detail);
    }

    private static async Task<Ok<List<WorkOrderSummaryDto>>> ListWorkOrders(
        HttpContext http, MaintenanceDbContext db, string? status, Guid? assetId, CancellationToken ct)
    {
        var query = Visible(db.WorkOrders.AsNoTracking(), http.User.ToCaller());
        if (MaintenanceValidation.IsName<WorkOrderStatus>(status))
        {
            var wanted = Enum.Parse<WorkOrderStatus>(status!);
            query = query.Where(w => w.Status == wanted);
        }

        if (assetId is { } asset)
        {
            query = query.Where(w => w.AssetId == asset);
        }

        var list = await query.OrderByDescending(w => w.UpdatedAtUtc).Take(500)
            .Select(w => new WorkOrderSummaryDto(w.Id, w.AssetId, w.AssetCode, w.Title, w.Priority.ToString(), w.Status.ToString(),
                w.AssignedTo, w.ScheduledFor, w.DueDate, w.Revision, w.UpdatedAtUtc))
            .ToListAsync(ct);
        return TypedResults.Ok(list);
    }

    private static async Task<Results<Ok<WorkOrderDetailDto>, NotFound>> GetWorkOrder(Guid id, HttpContext http, MaintenanceDbContext db, CancellationToken ct)
    {
        var detail = await LoadDetailAsync(db, id, http.User.ToCaller(), ct);
        return detail is null ? TypedResults.NotFound() : TypedResults.Ok(detail);
    }

    private static async Task<Results<Ok<WorkOrderDetailDto>, ValidationProblem, NotFound, ProblemHttpResult>> EditWorkOrder(
        Guid id, EditWorkOrderRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct)
    {
        var errors = MaintenanceValidation.Validate(body);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await ChangeAsync(id, body.Version, http, db, uow, clock,
            (w, _, caller, now) => w.Edit(caller, body.Title, body.Description, Enum.Parse<Priority>(body.Priority), body.DueDate, body.Checklist, now), ct);
        return result.Result switch
        {
            Ok<WorkOrderDetailDto> ok => ok,
            NotFound notFound => notFound,
            ProblemHttpResult problem => problem,
            _ => throw new InvalidOperationException("Unexpected result."),
        };
    }

    // ── Transitions ────────────────────────────────────────────────────────────────────────────────────────

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Approve(
        Guid id, VersionRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, r, c, now) => w.Approve(c, r, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Reject(
        Guid id, ReasonRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, r, c, now) => w.Reject(c, r, body.Reason ?? string.Empty, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Schedule(
        Guid id, ScheduleRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Schedule(c, body.Assignee, body.ScheduledFor, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Start(
        Guid id, VersionRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Start(c, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Hold(
        Guid id, ReasonRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Hold(c, body.Reason ?? string.Empty, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Resume(
        Guid id, VersionRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Resume(c, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Complete(
        Guid id, VersionRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Complete(c, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Close(
        Guid id, CloseRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Close(c, ParseDisposition(body.Disposition), now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> Cancel(
        Guid id, ReasonRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct) =>
        ChangeAsync(id, body.Version, http, db, uow, clock, (w, _, c, now) => w.Cancel(c, body.Reason ?? string.Empty, now), ct);

    private static Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> UpdateChecklistItem(
        Guid id, Guid itemId, ChecklistItemUpdate body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct)
    {
        if (!MaintenanceValidation.IsName<ChecklistItemStatus>(body.Status) || body.Note is { Length: > 1000 })
        {
            return Task.FromResult<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>>(
                TypedResults.Problem(statusCode: 400, title: "validation", detail: $"Status must be one of {string.Join(", ", Enum.GetNames<ChecklistItemStatus>())}; note at most 1000 characters."));
        }

        return ChangeAsync(id, body.Version, http, db, uow, clock,
            (w, _, c, now) => w.RecordChecklistItem(c, itemId, Enum.Parse<ChecklistItemStatus>(body.Status), body.Note, now), ct);
    }

    /// <summary>
    /// The one place a work order changes: load it (respecting visibility), check the client's version, let the
    /// domain apply the change, write history and audit in the same transaction, and map rule violations to HTTP.
    /// </summary>
    private static async Task<Results<Ok<WorkOrderDetailDto>, NotFound, ProblemHttpResult>> ChangeAsync(
        Guid id, uint version, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock,
        Action<WorkOrder, MaintenanceRequest, Caller, DateTimeOffset> change, CancellationToken ct)
    {
        var caller = http.User.ToCaller();
        try
        {
            var found = await uow.ExecuteAsync(async c =>
            {
                var workOrder = await Visible(db.WorkOrders, caller)
                    .Include(w => w.Checklist.OrderBy(i => i.Position))
                    .FirstOrDefaultAsync(w => w.Id == id, c);
                if (workOrder is null)
                {
                    return (false, (IReadOnlyList<AuditRecord>)[]);
                }

                db.Entry(workOrder).Property(w => w.Version).OriginalValue = version;
                var request = await db.Requests.FirstAsync(r => r.Id == workOrder.RequestId, c);
                var before = WorkOrderSnapshot.Of(workOrder);
                change(workOrder, request, caller, clock.GetUtcNow());
                TrackPending(db, workOrder);
                var action = workOrder.PendingHistory[^1].Action;
                return (true, (IReadOnlyList<AuditRecord>)[new(AuditType, workOrder.Id.ToString(), action, before, WorkOrderSnapshot.Of(workOrder))]);
            }, ct);

            return found ? TypedResults.Ok((await LoadDetailAsync(db, id, caller, ct))!) : TypedResults.NotFound();
        }
        catch (WorkOrderRuleException ex) when (ex.Code == "not-found")
        {
            return TypedResults.NotFound();
        }
        catch (WorkOrderRuleException ex)
        {
            return RuleProblem(ex);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(statusCode: 409, title: "stale-version",
                detail: "Someone else changed this work order. Reload it and try again.");
        }
    }

    // ── Findings and attachments ───────────────────────────────────────────────────────────────────────────

    private static async Task<Results<Created<FindingDto>, ValidationProblem, NotFound, ProblemHttpResult>> CreateFinding(
        Guid id, CreateFindingRequest body, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow, TimeProvider clock, CancellationToken ct)
    {
        var errors = MaintenanceValidation.Validate(body);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var caller = http.User.ToCaller();
        try
        {
            var finding = await uow.ExecuteAsync(async c =>
            {
                var workOrder = await Visible(db.WorkOrders, caller).Include(w => w.Checklist).FirstOrDefaultAsync(w => w.Id == id, c);
                if (workOrder is null)
                {
                    return ((Finding?)null, (IReadOnlyList<AuditRecord>)[]);
                }

                workOrder.EnsureCanRecordFinding(caller);
                var before = WorkOrderSnapshot.Of(workOrder);
                var now = clock.GetUtcNow();
                var created = new Finding(workOrder.Id, caller.Username, Enum.Parse<ConditionLabel>(body.Condition), body.Notes.Trim(), now);
                db.Findings.Add(created);
                workOrder.NoteFinding(caller, created.Condition.ToString(), now);
                TrackPending(db, workOrder);
                return ((Finding?)created, (IReadOnlyList<AuditRecord>)
                [
                    new("Finding", created.Id.ToString(), "Recorded", null, new { WorkOrderId = workOrder.Id, Condition = created.Condition.ToString(), created.Notes }),
                    new(AuditType, workOrder.Id.ToString(), "Finding", before, WorkOrderSnapshot.Of(workOrder)),
                ]);
            }, ct);

            return finding is null
                ? TypedResults.NotFound()
                : TypedResults.Created($"{MaintenanceModule.RoutePrefix}/work-orders/{id}", ToDto(finding));
        }
        catch (WorkOrderRuleException ex)
        {
            return RuleProblem(ex);
        }
    }

    private static async Task<Results<Created<AttachmentDto>, NotFound, ProblemHttpResult>> UploadAttachment(
        Guid id, Guid findingId, IFormFile file, HttpContext http, MaintenanceDbContext db, MaintenanceUnitOfWork uow,
        IAttachmentStore store, ICurrentTenant tenant, TimeProvider clock, CancellationToken ct)
    {
        var caller = http.User.ToCaller();
        var workOrder = await Visible(db.WorkOrders.AsNoTracking(), caller).FirstOrDefaultAsync(w => w.Id == id, ct);
        var finding = workOrder is null ? null : await db.Findings.AsNoTracking().Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == findingId && f.WorkOrderId == id, ct);
        if (workOrder is null || finding is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            workOrder.EnsureCanRecordFinding(caller);
        }
        catch (WorkOrderRuleException ex)
        {
            return RuleProblem(ex);
        }

        if (finding.Attachments.Count >= MaxAttachmentsPerFinding)
        {
            return TypedResults.Problem(statusCode: 409, title: "too-many-attachments", detail: $"A finding has at most {MaxAttachmentsPerFinding} attachments.");
        }

        // Size first, before reading anything: the limit protects memory and storage.
        if (file.Length is 0 or > FileSignatures.MaxBytes)
        {
            return TypedResults.Problem(statusCode: 413, title: "file-size", detail: $"Files must be between 1 byte and {FileSignatures.MaxBytes / (1024 * 1024)} MB.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await using (var upload = file.OpenReadStream())
        {
            await upload.CopyToAsync(buffer, ct);
        }

        var detected = FileSignatures.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, FileSignatures.HeaderLength)));
        if (detected is not { } type)
        {
            // The declared Content-Type and the extension are ignored on purpose: only the bytes count.
            return TypedResults.Problem(statusCode: 415, title: "file-type",
                detail: $"Only {string.Join(", ", FileSignatures.AllowedContentTypes)} are accepted (checked from the file's content, not its name).");
        }

        var now = clock.GetUtcNow();
        var key = $"{tenant.TenantId}/{now:yyyy}/{now:MM}/{Guid.CreateVersion7():N}{type.Extension}";
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
        buffer.Position = 0;
        await store.SaveAsync(key, buffer, ct);

        try
        {
            var attachment = await uow.ExecuteAsync(_ =>
            {
                var created = new Attachment(finding.Id, FileSignatures.SafeDisplayName(file.FileName, type.Extension), type.ContentType, buffer.Length, sha256, key, now);
                db.Attachments.Add(created);
                IReadOnlyList<AuditRecord> audit =
                [
                    new("Attachment", created.Id.ToString(), "Uploaded", null, new { FindingId = finding.Id, created.FileName, created.ContentType, created.SizeBytes, created.Sha256 }),
                ];
                return Task.FromResult((created, audit));
            }, ct);
            return TypedResults.Created($"{MaintenanceModule.RoutePrefix}/attachments/{attachment.Id}", ToDto(attachment));
        }
        catch
        {
            // Why: bytes are written before the transaction; if the row is not committed, remove the orphan file.
            await store.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> DownloadAttachment(
        Guid id, HttpContext http, MaintenanceDbContext db, IAttachmentStore store, CancellationToken ct)
    {
        var caller = http.User.ToCaller();
        var attachment = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attachment is null)
        {
            return TypedResults.NotFound();
        }

        var workOrderId = await db.Findings.AsNoTracking().Where(f => f.Id == attachment.FindingId).Select(f => f.WorkOrderId).FirstAsync(ct);
        if (!await Visible(db.WorkOrders.AsNoTracking(), caller).AnyAsync(w => w.Id == workOrderId, ct))
        {
            return TypedResults.NotFound();
        }

        var stream = await store.OpenReadAsync(attachment.StorageKey, ct);
        if (stream is null)
        {
            return TypedResults.NotFound();
        }

        // Why nosniff + download: the browser must not reinterpret an upload as HTML or script.
        http.Response.Headers.XContentTypeOptions = "nosniff";
        return TypedResults.File(stream, attachment.ContentType, attachment.FileName);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Row visibility on top of tenancy: technicians see only work assigned to them.</summary>
    private static IQueryable<WorkOrder> Visible(IQueryable<WorkOrder> query, Caller caller) =>
        caller.SeesOnlyOwnWork() ? query.Where(w => w.AssignedTo == caller.Username) : query;

    /// <summary>Adds what the domain created or removed explicitly (see learning note 03: explicit beats clever).</summary>
    private static void TrackPending(MaintenanceDbContext db, WorkOrder workOrder)
    {
        db.History.AddRange(workOrder.PendingHistory.Where(h => db.Entry(h).State == EntityState.Detached));
        db.ChecklistItems.AddRange(workOrder.PendingChecklist.Where(i => db.Entry(i).State == EntityState.Detached));
        db.ChecklistItems.RemoveRange(workOrder.RemovedChecklist.Where(i => db.Entry(i).State != EntityState.Detached));
    }

    private static Disposition? ParseDisposition(string? value) =>
        MaintenanceValidation.IsName<Disposition>(value) ? Enum.Parse<Disposition>(value!) : null;

    private static ProblemHttpResult RuleProblem(WorkOrderRuleException ex) => TypedResults.Problem(
        statusCode: ex.Code switch
        {
            "validation" => 400,
            "forbidden" or "not-assigned" => 403,
            "not-found" => 404,
            _ => 409,
        },
        title: ex.Code,
        detail: ex.Message);

    private static async Task<WorkOrderDetailDto?> LoadDetailAsync(MaintenanceDbContext db, Guid id, Caller caller, CancellationToken ct)
    {
        var w = await Visible(db.WorkOrders.AsNoTracking(), caller)
            .Include(x => x.Checklist)
            .Include(x => x.History)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null)
        {
            return null;
        }

        var request = await db.Requests.AsNoTracking().FirstAsync(r => r.Id == w.RequestId, ct);
        var findings = await db.Findings.AsNoTracking().Include(f => f.Attachments)
            .Where(f => f.WorkOrderId == id).OrderBy(f => f.RecordedAtUtc).ToListAsync(ct);

        return new WorkOrderDetailDto(
            w.Id, w.AssetId, w.AssetCode, w.Title, w.Description, w.Priority.ToString(), w.Status.ToString(), w.StatusReason,
            w.AssignedTo, w.ScheduledFor, w.DueDate, w.ApprovedBy, w.Disposition?.ToString(), w.Revision, w.Version,
            w.CreatedAtUtc, w.UpdatedAtUtc,
            new RequestInfoDto(request.Id, request.Reason, request.RequestedBy, request.RequestedAtUtc, request.Decision.ToString()),
            [.. w.Checklist.OrderBy(c => c.Position).Select(c => new ChecklistItemDto(c.Id, c.Position, c.Text, c.Status.ToString(), c.Note))],
            [.. w.History.OrderBy(h => h.Revision).Select(h => new HistoryEntryDto(h.Revision, h.Action, h.FromStatus.ToString(), h.ToStatus.ToString(), h.Actor, h.Note, h.AtUtc))],
            [.. findings.Select(ToDto)],
            [.. w.AvailableActions(caller).Select(a => a.ToString())]);
    }

    private static FindingDto ToDto(Finding f) => new(f.Id, f.Technician, f.Condition.ToString(), f.Notes, f.RecordedAtUtc,
        [.. f.Attachments.OrderBy(a => a.UploadedAtUtc).Select(ToDto)]);

    private static AttachmentDto ToDto(Attachment a) => new(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAtUtc);
}
