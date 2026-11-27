using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Modules.Audit.Persistence;

namespace SmartFactory.Modules.Audit.Endpoints;

internal sealed record AuditEventDto(
    long Sequence, DateTimeOffset OccurredAtUtc, string ActorId, string ActorName, string AggregateType, string AggregateId,
    string Action, string? BeforeState, string? AfterState, string? BeforeHash, string? AfterHash, string CorrelationId, string ChainHash);

internal sealed record AuditVerificationDto(bool Intact, long EventsChecked, long? FirstBrokenSequence);

/// <summary>
/// Read-only HTTP surface of the audit trail. Reading is for the reliability manager and the administrator;
/// verifying the hash chain is for the administrator (blueprint RBAC, p.6). There is deliberately no write endpoint.
/// </summary>
internal static class AuditEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(AuditModule.RoutePrefix).WithTags("Audit");

        group.MapGet("/events", ListEvents).WithName("Audit.ListEvents")
            .WithSummary("Audit events of the caller's tenant, newest first, optionally for one aggregate.")
            .RequireAuthorization(Policies.ReliabilityManager);
        group.MapGet("/verify", Verify).WithName("Audit.Verify")
            .WithSummary("Recompute the tenant's hash chain and report the first broken record, if any.")
            .RequireAuthorization(Policies.Administrator);
    }

    private static async Task<Ok<List<AuditEventDto>>> ListEvents(
        AuditDbContext db, string? aggregateType, string? aggregateId, int? take, CancellationToken ct)
    {
        var query = db.Events.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(aggregateType))
        {
            query = query.Where(e => e.AggregateType == aggregateType);
        }

        if (!string.IsNullOrWhiteSpace(aggregateId))
        {
            query = query.Where(e => e.AggregateId == aggregateId);
        }

        var limit = Math.Clamp(take ?? 50, 1, 500);
        var events = await query.OrderByDescending(e => e.Sequence).Take(limit)
            .Select(e => new AuditEventDto(e.Sequence, e.OccurredAtUtc, e.ActorId, e.ActorName, e.AggregateType, e.AggregateId,
                e.Action, e.BeforeState, e.AfterState, e.BeforeHash, e.AfterHash, e.CorrelationId, e.ChainHash))
            .ToListAsync(ct);
        return TypedResults.Ok(events);
    }

    private static async Task<Ok<AuditVerificationDto>> Verify(AuditDbContext db, CancellationToken ct)
    {
        string? previous = null;
        long checkedCount = 0;
        long expectedSequence = 1;
        await foreach (var e in db.Events.AsNoTracking().OrderBy(e => e.Sequence).AsAsyncEnumerable().WithCancellation(ct))
        {
            checkedCount++;
            if (e.Sequence != expectedSequence || !e.IsIntact(previous))
            {
                return TypedResults.Ok(new AuditVerificationDto(false, checkedCount, e.Sequence));
            }

            previous = e.ChainHash;
            expectedSequence++;
        }

        return TypedResults.Ok(new AuditVerificationDto(true, checkedCount, null));
    }
}
