using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartFactory.BuildingBlocks.Auditing;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets.Domain;
using SmartFactory.Modules.Assets.Persistence;

namespace SmartFactory.Modules.Assets.Endpoints;

/// <summary>
/// HTTP surface of the Assets module. Reads need any authenticated tenant member; writes need the administrator
/// persona (blueprint RBAC matrix, p.6). Tenancy is never a parameter: it comes from the token, and EF filters plus
/// row-level security make another tenant's rows invisible, so a foreign id answers 404, not 403.
/// </summary>
internal static class AssetEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(AssetsModule.RoutePrefix).WithTags("Assets").RequireAuthorization(Policies.TenantMember);

        group.MapGet("/plants", ListPlants).WithName("Assets.ListPlants")
            .WithSummary("Plants of the caller's tenant with their lines and asset counts.");
        group.MapGet("/assets", ListAssets).WithName("Assets.ListAssets")
            .WithSummary("Assets of the caller's tenant, optionally filtered by line, criticality or text.");
        group.MapGet("/assets/{id:guid}", GetAsset).WithName("Assets.GetAsset")
            .WithSummary("One asset with its sensor mappings.");

        group.MapPost("/assets", CreateAsset).WithName("Assets.CreateAsset")
            .WithSummary("Register an asset on a line (administrator).").RequireAuthorization(Policies.Administrator);
        group.MapPut("/assets/{id:guid}", UpdateAsset).WithName("Assets.UpdateAsset")
            .WithSummary("Update an asset; send the version you read to avoid overwriting someone else's change (administrator).")
            .RequireAuthorization(Policies.Administrator);
        group.MapPost("/assets/{id:guid}/sensors", AddSensor).WithName("Assets.AddSensor")
            .WithSummary("Map a new sensor to an asset (administrator).").RequireAuthorization(Policies.Administrator);
        group.MapDelete("/assets/{id:guid}/sensors/{sensorId:guid}", RemoveSensor).WithName("Assets.RemoveSensor")
            .WithSummary("Remove a sensor mapping (administrator).").RequireAuthorization(Policies.Administrator);
    }

    private static async Task<Ok<List<PlantDto>>> ListPlants(AssetsDbContext db, CancellationToken ct)
    {
        var plants = await db.Plants.AsNoTracking().OrderBy(p => p.Code)
            .Select(p => new PlantDto(p.Id, p.Code, p.Name, p.TimeZone,
                p.Lines.OrderBy(l => l.Code).Select(l => new LineDto(l.Id, l.Code, l.Name, l.Assets.Count)).ToList()))
            .ToListAsync(ct);
        return TypedResults.Ok(plants);
    }

    private static async Task<Ok<List<AssetSummaryDto>>> ListAssets(
        AssetsDbContext db, Guid? lineId, string? criticality, string? search, CancellationToken ct)
    {
        var query = db.Assets.AsNoTracking();
        if (lineId is { } line)
        {
            query = query.Where(a => a.LineId == line);
        }

        if (AssetValidation.IsName<Criticality>(criticality))
        {
            var level = Enum.Parse<Criticality>(criticality!);
            query = query.Where(a => a.Criticality == level);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Code, pattern) || EF.Functions.ILike(a.Name, pattern));
        }

        var assets = await query.OrderBy(a => a.Code)
            .Select(a => new AssetSummaryDto(a.Id, a.Code, a.Name, a.AssetClass, a.Criticality.ToString(), a.Status.ToString(),
                a.LineId, a.Line!.Code, a.Line.Name, a.Line.Plant!.Code, a.Sensors.Count))
            .ToListAsync(ct);
        return TypedResults.Ok(assets);
    }

    private static async Task<Results<Ok<AssetDetailDto>, NotFound>> GetAsset(Guid id, AssetsDbContext db, CancellationToken ct)
    {
        var asset = await LoadDetail(db, id, ct);
        return asset is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetail(asset));
    }

    private static async Task<Results<Created<AssetDetailDto>, ValidationProblem, NotFound, Conflict<ProblemDetailsBody>>> CreateAsset(
        CreateAssetRequest request, AssetsDbContext db, AssetsUnitOfWork unitOfWork, CancellationToken ct)
    {
        var errors = AssetValidation.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!await db.Lines.AnyAsync(l => l.Id == request.LineId, ct))
        {
            return TypedResults.NotFound();
        }

        try
        {
            var asset = await unitOfWork.ExecuteAsync(_ =>
            {
                var created = new Asset(request.LineId, request.Code, request.Name.Trim(), request.AssetClass,
                    Enum.Parse<Criticality>(request.Criticality), request.Description, request.RatedPowerKw);
                db.Assets.Add(created);
                IReadOnlyList<AuditRecord> audit = [new("Asset", created.Id.ToString(), "Created", null, AssetSnapshot.Of(created))];
                return Task.FromResult((created, audit));
            }, ct);

            var detail = await LoadDetail(db, asset.Id, ct);
            return TypedResults.Created($"{AssetsModule.RoutePrefix}/assets/{asset.Id}", ToDetail(detail!));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict(new ProblemDetailsBody("Duplicate asset code", $"An asset with code {request.Code} already exists."));
        }
    }

    private static async Task<Results<Ok<AssetDetailDto>, ValidationProblem, NotFound, Conflict<ProblemDetailsBody>>> UpdateAsset(
        Guid id, UpdateAssetRequest request, AssetsDbContext db, AssetsUnitOfWork unitOfWork, CancellationToken ct)
    {
        var errors = AssetValidation.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            await unitOfWork.ExecuteAsync(_ =>
            {
                var before = AssetSnapshot.Of(asset);
                // Why OriginalValue: EF puts it in the UPDATE's WHERE clause (xmin = @version). If someone else saved
                // in between, no row matches and EF throws DbUpdateConcurrencyException instead of overwriting.
                db.Entry(asset).Property(a => a.Version).OriginalValue = request.Version;
                asset.Update(request.Name.Trim(), Enum.Parse<Criticality>(request.Criticality), Enum.Parse<AssetStatus>(request.Status),
                    request.Description, request.RatedPowerKw);
                IReadOnlyList<AuditRecord> audit = [new("Asset", asset.Id.ToString(), "Updated", before, AssetSnapshot.Of(asset))];
                return Task.FromResult((true, audit));
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Conflict(new ProblemDetailsBody("Asset changed", "Someone else changed this asset. Reload it and try again."));
        }

        db.ChangeTracker.Clear();
        return TypedResults.Ok(ToDetail((await LoadDetail(db, id, ct))!));
    }

    private static async Task<Results<Created<SensorDto>, ValidationProblem, NotFound, Conflict<ProblemDetailsBody>>> AddSensor(
        Guid id, CreateSensorRequest request, AssetsDbContext db, AssetsUnitOfWork unitOfWork, ICurrentTenant tenant, CancellationToken ct)
    {
        var errors = AssetValidation.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var asset = await db.Assets.Include(a => a.Line!).ThenInclude(l => l.Plant).SingleOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var sensor = await unitOfWork.ExecuteAsync(_ =>
            {
                var topic = SensorMapping.BuildTopic(tenant.TenantId!, asset.Line!.Plant!.Code, asset.Line.Code, asset.Code, request.Metric);
                var created = new SensorMapping(asset.Id, request.Metric, request.Unit.Trim(), request.IntervalSeconds,
                    request.NominalMin, request.NominalMax, request.Kind, topic);
                db.SensorMappings.Add(created);
                IReadOnlyList<AuditRecord> audit = [new("SensorMapping", created.Id.ToString(), "Created", null, SensorSnapshot.Of(created))];
                return Task.FromResult((created, audit));
            }, ct);
            return TypedResults.Created($"{AssetsModule.RoutePrefix}/assets/{id}", ToSensor(sensor));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict(new ProblemDetailsBody("Duplicate sensor", $"Asset {asset.Code} already maps metric {request.Metric}."));
        }
    }

    private static async Task<Results<NoContent, NotFound>> RemoveSensor(
        Guid id, Guid sensorId, AssetsDbContext db, AssetsUnitOfWork unitOfWork, CancellationToken ct)
    {
        var sensor = await db.SensorMappings.SingleOrDefaultAsync(s => s.Id == sensorId && s.AssetId == id, ct);
        if (sensor is null)
        {
            return TypedResults.NotFound();
        }

        await unitOfWork.ExecuteAsync(_ =>
        {
            var before = SensorSnapshot.Of(sensor);
            db.SensorMappings.Remove(sensor);
            IReadOnlyList<AuditRecord> audit = [new("SensorMapping", sensor.Id.ToString(), "Deleted", before, null)];
            return Task.FromResult((true, audit));
        }, ct);
        return TypedResults.NoContent();
    }

    private static Task<Asset?> LoadDetail(AssetsDbContext db, Guid id, CancellationToken ct) =>
        db.Assets.AsNoTracking()
            .Include(a => a.Line!).ThenInclude(l => l.Plant)
            .Include(a => a.Sensors.OrderBy(s => s.Metric))
            .SingleOrDefaultAsync(a => a.Id == id, ct);

    private static AssetDetailDto ToDetail(Asset a) => new(
        a.Id, a.Code, a.Name, a.AssetClass, a.Criticality.ToString(), a.Status.ToString(), a.Description, a.RatedPowerKw,
        a.LineId, a.Line!.Code, a.Line.Name, a.Line.Plant!.Code, a.Version, a.UpdatedAtUtc,
        a.Sensors.Select(ToSensor).ToList());

    private static SensorDto ToSensor(SensorMapping s) =>
        new(s.Id, s.Metric, s.Unit, s.IntervalSeconds, s.NominalMin, s.NominalMax, s.Kind, s.SourceTopic);
}

/// <summary>Minimal RFC 9457-style body for 409 responses.</summary>
internal sealed record ProblemDetailsBody(string Title, string Detail);
