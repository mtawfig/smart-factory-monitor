using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SmartFactory.Modules.Assets.Seeding;

/// <summary>
/// The subset of <c>docs/plant/plant.yaml</c> the asset registry needs. The Python simulator reads the same file with
/// its own model; the YAML is the contract between them (ADR-001: contracts, not shared code).
/// </summary>
internal sealed class PlantSpecification
{
    public int SpecVersion { get; set; }
    public bool Synthetic { get; set; }
    public TenantSpec Tenant { get; set; } = new();
    public PlantSpec Plant { get; set; } = new();
    public List<LineSpec> Lines { get; set; } = [];

    public static PlantSpecification Load(string path)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        using var reader = File.OpenText(path);
        var spec = deserializer.Deserialize<PlantSpecification>(reader)
                   ?? throw new InvalidDataException($"Plant specification '{path}' is empty.");
        spec.Validate(path);
        return spec;
    }

    private void Validate(string path)
    {
        // Why: the content rules require synthetic data to be labelled as such; a spec without the flag is refused.
        if (!Synthetic)
        {
            throw new InvalidDataException($"Plant specification '{path}' must declare synthetic: true.");
        }

        if (string.IsNullOrWhiteSpace(Tenant.Id) || string.IsNullOrWhiteSpace(Plant.Id) || Lines.Count == 0)
        {
            throw new InvalidDataException($"Plant specification '{path}' needs tenant.id, plant.id and at least one line.");
        }
    }
}

internal sealed class TenantSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

internal sealed class PlantSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}

internal sealed class LineSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<AssetSpec> Assets { get; set; } = [];
}

internal sealed class AssetSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public string Criticality { get; set; } = "Medium";
    public string? Description { get; set; }
    public decimal? RatedPowerKw { get; set; }
    public List<SensorSpec> Sensors { get; set; } = [];
}

internal sealed class SensorSpec
{
    public string Metric { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal IntervalS { get; set; }
    public List<decimal>? Nominal { get; set; }
    public string? Kind { get; set; }
}
