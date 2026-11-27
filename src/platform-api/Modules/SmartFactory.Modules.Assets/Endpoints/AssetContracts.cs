using SmartFactory.Modules.Assets.Domain;

namespace SmartFactory.Modules.Assets.Endpoints;

// HTTP contracts of the Assets module. They are the module's public API over the wire and are frozen in
// contracts/openapi/platform-api.v1.json; the OpenAPI contract test fails if they change unintentionally.

internal sealed record PlantDto(Guid Id, string Code, string Name, string TimeZone, IReadOnlyList<LineDto> Lines);

internal sealed record LineDto(Guid Id, string Code, string Name, int AssetCount);

internal sealed record AssetSummaryDto(
    Guid Id, string Code, string Name, string AssetClass, string Criticality, string Status,
    Guid LineId, string LineCode, string LineName, string PlantCode, int SensorCount);

internal sealed record SensorDto(
    Guid Id, string Metric, string Unit, decimal IntervalSeconds, decimal? NominalMin, decimal? NominalMax, string? Kind, string SourceTopic);

internal sealed record AssetDetailDto(
    Guid Id, string Code, string Name, string AssetClass, string Criticality, string Status, string? Description,
    decimal? RatedPowerKw, Guid LineId, string LineCode, string LineName, string PlantCode, uint Version,
    DateTimeOffset UpdatedAtUtc, IReadOnlyList<SensorDto> Sensors);

internal sealed record CreateAssetRequest(Guid LineId, string Code, string Name, string AssetClass, string Criticality, string? Description, decimal? RatedPowerKw);

internal sealed record UpdateAssetRequest(string Name, string Criticality, string Status, string? Description, decimal? RatedPowerKw, uint Version);

internal sealed record CreateSensorRequest(string Metric, string Unit, decimal IntervalSeconds, decimal? NominalMin, decimal? NominalMax, string? Kind);

/// <summary>What the audit trail stores for an asset: its business state, without navigation properties.</summary>
internal sealed record AssetSnapshot(
    string Code, string Name, string AssetClass, string Criticality, string Status, string? Description, decimal? RatedPowerKw, Guid LineId)
{
    public static AssetSnapshot Of(Asset a) =>
        new(a.Code, a.Name, a.AssetClass, a.Criticality.ToString(), a.Status.ToString(), a.Description, a.RatedPowerKw, a.LineId);
}

internal sealed record SensorSnapshot(Guid AssetId, string Metric, string Unit, decimal IntervalSeconds, decimal? NominalMin, decimal? NominalMax, string? Kind, string SourceTopic)
{
    public static SensorSnapshot Of(SensorMapping s) =>
        new(s.AssetId, s.Metric, s.Unit, s.IntervalSeconds, s.NominalMin, s.NominalMax, s.Kind, s.SourceTopic);
}

internal static class AssetValidation
{
    public static Dictionary<string, string[]> Validate(CreateAssetRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Asset.IsValidCode(r.Code))
        {
            errors["code"] = ["Code must look like ABC-123."];
        }

        if (!AssetClasses.All.Contains(r.AssetClass ?? string.Empty))
        {
            errors["assetClass"] = [$"Asset class must be one of {string.Join(", ", AssetClasses.All.Order())}."];
        }

        ValidateCommon(errors, r.Name, r.Criticality, status: null, r.Description, r.RatedPowerKw);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(UpdateAssetRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateCommon(errors, r.Name, r.Criticality, r.Status, r.Description, r.RatedPowerKw);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(CreateSensorRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (!SensorMapping.IsValidMetric(r.Metric))
        {
            errors["metric"] = ["Metric must be lower_snake_case, 2–41 characters."];
        }

        if (string.IsNullOrWhiteSpace(r.Unit) || r.Unit.Length > 16)
        {
            errors["unit"] = ["Unit is required (max 16 characters)."];
        }

        if (r.IntervalSeconds is <= 0 or > 3600)
        {
            errors["intervalSeconds"] = ["Interval must be between 0 and 3600 seconds."];
        }

        if (r.NominalMin.HasValue != r.NominalMax.HasValue || (r.NominalMin.HasValue && r.NominalMin >= r.NominalMax))
        {
            errors["nominal"] = ["Give both nominal bounds with min < max, or neither."];
        }

        return errors;
    }

    /// <summary>Exact enum name only; <c>Enum.TryParse</c> would also accept numbers such as "7".</summary>
    public static bool IsName<TEnum>(string? value) where TEnum : struct, Enum =>
        value is not null && Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal);

    private static void ValidateCommon(Dictionary<string, string[]> errors, string? name, string? criticality, string? status, string? description, decimal? ratedPowerKw)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            errors["name"] = ["Name is required (max 120 characters)."];
        }

        if (!IsName<Criticality>(criticality))
        {
            errors["criticality"] = ["Criticality must be Low, Medium or High."];
        }

        if (status is not null && !IsName<AssetStatus>(status))
        {
            errors["status"] = ["Status must be InService, OutOfService or Decommissioned."];
        }

        if (description is { Length: > 1000 })
        {
            errors["description"] = ["Description is limited to 1000 characters."];
        }

        if (ratedPowerKw is < 0 or > 100000)
        {
            errors["ratedPowerKw"] = ["Rated power must be between 0 and 100000 kW."];
        }
    }
}
