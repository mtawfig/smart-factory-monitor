using System.Text.RegularExpressions;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.Modules.Assets.Domain;

/// <summary>A customer of the platform. The root of all tenant-scoped data; its id is the <c>tenant_id</c> claim.</summary>
internal sealed class Tenant
{
    private Tenant() { }

    public Tenant(string id, string name)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

/// <summary>A site, e.g. "Al Safa Industrial — Plant 01".</summary>
internal sealed class Plant : ITenantScoped
{
    private Plant() { }

    public Plant(string code, string name, string timeZone)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Name = name;
        TimeZone = timeZone;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = string.Empty;
    public List<Line> Lines { get; private set; } = [];
}

/// <summary>A production line inside a plant.</summary>
internal sealed class Line : ITenantScoped
{
    private Line() { }

    public Line(Guid plantId, string code, string name)
    {
        Id = Guid.CreateVersion7();
        PlantId = plantId;
        Code = code;
        Name = name;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid PlantId { get; private set; }
    public Plant? Plant { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public List<Asset> Assets { get; private set; } = [];
}

internal enum Criticality { Low, Medium, High }

internal enum AssetStatus { InService, OutOfService, Decommissioned }

internal static class AssetClasses
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "MOTOR", "CONVEYOR", "COMPRESSOR", "FAN", "CNC", "PUMP", "PRESS", "CHILLER",
    };
}

/// <summary>An instrumented machine. Codes follow the plant convention <c>ABC-123</c> and are unique per tenant.</summary>
internal sealed partial class Asset : ITenantScoped
{
    private Asset() { }

    public Asset(Guid lineId, string code, string name, string assetClass, Criticality criticality, string? description, decimal? ratedPowerKw)
    {
        Id = Guid.CreateVersion7();
        LineId = lineId;
        Code = code;
        AssetClass = assetClass;
        Status = AssetStatus.InService;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Update(name, criticality, Status, description, ratedPowerKw);
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid LineId { get; private set; }
    public Line? Line { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string AssetClass { get; private set; } = string.Empty;
    public Criticality Criticality { get; private set; }
    public AssetStatus Status { get; private set; }
    public string? Description { get; private set; }
    public decimal? RatedPowerKw { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>PostgreSQL <c>xmin</c>: changes on every update; used for optimistic concurrency.</summary>
    public uint Version { get; private set; }

    public List<SensorMapping> Sensors { get; private set; } = [];

    public void Update(string name, Criticality criticality, AssetStatus status, string? description, decimal? ratedPowerKw)
    {
        Name = name;
        Criticality = criticality;
        Status = status;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RatedPowerKw = ratedPowerKw;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static bool IsValidCode(string? code) => code is not null && CodePattern().IsMatch(code);

    [GeneratedRegex("^[A-Z]{3}-[0-9]{3}$")]
    private static partial Regex CodePattern();
}

/// <summary>
/// Maps one physical measurement of an asset to its MQTT source topic (ADR-004) and its nominal operating band.
/// </summary>
internal sealed partial class SensorMapping : ITenantScoped
{
    private SensorMapping() { }

    public SensorMapping(Guid assetId, string metric, string unit, decimal intervalSeconds, decimal? nominalMin, decimal? nominalMax, string? kind, string sourceTopic)
    {
        Id = Guid.CreateVersion7();
        AssetId = assetId;
        Metric = metric;
        Unit = unit;
        IntervalSeconds = intervalSeconds;
        NominalMin = nominalMin;
        NominalMax = nominalMax;
        Kind = kind;
        SourceTopic = sourceTopic;
        ActiveFromUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid AssetId { get; private set; }
    public string Metric { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal IntervalSeconds { get; private set; }
    public decimal? NominalMin { get; private set; }
    public decimal? NominalMax { get; private set; }
    public string? Kind { get; private set; }
    public string SourceTopic { get; private set; } = string.Empty;
    public DateTimeOffset ActiveFromUtc { get; private set; }

    /// <summary>ADR-004 topic scheme: <c>plant/{tenant}/{plant}/{line}/{asset}/{metric}</c>.</summary>
    public static string BuildTopic(string tenantId, string plantCode, string lineCode, string assetCode, string metric) =>
        $"plant/{tenantId}/{plantCode}/{lineCode}/{assetCode}/{metric}";

    public static bool IsValidMetric(string? metric) => metric is not null && MetricPattern().IsMatch(metric);

    [GeneratedRegex("^[a-z][a-z0-9_]{1,40}$")]
    private static partial Regex MetricPattern();
}
