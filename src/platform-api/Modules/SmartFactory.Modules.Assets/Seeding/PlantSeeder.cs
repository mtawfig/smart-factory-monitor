using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartFactory.BuildingBlocks.Auditing;
using SmartFactory.Modules.Assets.Domain;
using SmartFactory.Modules.Assets.Endpoints;
using SmartFactory.Modules.Assets.Persistence;

namespace SmartFactory.Modules.Assets.Seeding;

/// <summary>
/// Creates the tenant, plant, lines, assets and sensor mappings described by <c>plant.yaml</c>. Idempotent: existing
/// records (matched by their codes) are left untouched, so running it twice creates nothing the second time. Every
/// record it creates is audited like any other change, with the actor <c>system:seed</c>.
/// Runs inside a <c>TenantScope</c>, so it writes through row-level security like a normal request.
/// Why explicit <c>db.X.Add</c>: entities with a preset Guid key that EF only discovers through a navigation can be
/// mistaken for existing rows and updated instead of inserted.
/// </summary>
internal sealed partial class PlantSeeder(AssetsDbContext db, AssetsUnitOfWork unitOfWork, ILogger<PlantSeeder> logger)
{
    public async Task<int> SeedAsync(PlantSpecification spec, string tenantId, CancellationToken cancellationToken)
    {
        var created = await unitOfWork.ExecuteAsync(async ct =>
        {
            var audit = new List<AuditRecord>();

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId, ct).ConfigureAwait(false))
            {
                var name = string.IsNullOrWhiteSpace(spec.Tenant.Name) ? tenantId : spec.Tenant.Name;
                db.Tenants.Add(new Tenant(tenantId, name));
                // Why save now: plants reference the tenant row by foreign key.
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                audit.Add(new AuditRecord("Tenant", tenantId, "Created", null, new { Id = tenantId, Name = name }));
            }

            var plant = await db.Plants.Include(p => p.Lines).ThenInclude(l => l.Assets).ThenInclude(a => a.Sensors)
                .SingleOrDefaultAsync(p => p.Code == spec.Plant.Id, ct).ConfigureAwait(false);
            if (plant is null)
            {
                plant = new Plant(spec.Plant.Id, spec.Plant.Name, spec.Plant.Timezone);
                db.Plants.Add(plant);
                audit.Add(new AuditRecord("Plant", plant.Id.ToString(), "Created", null, new { plant.Code, plant.Name, plant.TimeZone }));
            }

            foreach (var lineSpec in spec.Lines)
            {
                var line = plant.Lines.SingleOrDefault(l => l.Code == lineSpec.Id);
                if (line is null)
                {
                    line = new Line(plant.Id, lineSpec.Id, lineSpec.Name);
                    db.Lines.Add(line);
                    plant.Lines.Add(line);
                    audit.Add(new AuditRecord("Line", line.Id.ToString(), "Created", null, new { line.PlantId, line.Code, line.Name }));
                }

                foreach (var assetSpec in lineSpec.Assets)
                {
                    var asset = line.Assets.SingleOrDefault(a => a.Code == assetSpec.Id);
                    if (asset is null)
                    {
                        if (!AssetValidation.IsName<Criticality>(assetSpec.Criticality))
                        {
                            throw new InvalidDataException($"Asset {assetSpec.Id}: unknown criticality '{assetSpec.Criticality}'.");
                        }

                        asset = new Asset(line.Id, assetSpec.Id, assetSpec.Name, assetSpec.AssetClass,
                            Enum.Parse<Criticality>(assetSpec.Criticality), assetSpec.Description, assetSpec.RatedPowerKw);
                        db.Assets.Add(asset);
                        line.Assets.Add(asset);
                        audit.Add(new AuditRecord("Asset", asset.Id.ToString(), "Created", null, AssetSnapshot.Of(asset)));
                    }

                    foreach (var sensorSpec in assetSpec.Sensors)
                    {
                        if (asset.Sensors.Any(s => s.Metric == sensorSpec.Metric))
                        {
                            continue;
                        }

                        var nominal = sensorSpec.Nominal is { Count: 2 } band ? band : null;
                        var sensor = new SensorMapping(asset.Id, sensorSpec.Metric, sensorSpec.Unit, sensorSpec.IntervalS,
                            nominal?[0], nominal?[1], sensorSpec.Kind,
                            SensorMapping.BuildTopic(tenantId, plant.Code, line.Code, asset.Code, sensorSpec.Metric));
                        db.SensorMappings.Add(sensor);
                        asset.Sensors.Add(sensor);
                        audit.Add(new AuditRecord("SensorMapping", sensor.Id.ToString(), "Created", null, SensorSnapshot.Of(sensor)));
                    }
                }
            }

            return (audit.Count, (IReadOnlyList<AuditRecord>)audit);
        }, cancellationToken).ConfigureAwait(false);

        LogSeeded(logger, tenantId, spec.Plant.Id, created);
        return created;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Plant seed for tenant {TenantId}, plant {PlantCode}: {Created} records created (synthetic data)")]
    private static partial void LogSeeded(ILogger logger, string tenantId, string plantCode, int created);
}
