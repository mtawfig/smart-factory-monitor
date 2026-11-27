namespace SmartFactory.Ingest.Worker;

/// <summary>Configuration of the ingest pipeline (section <c>Ingest</c>).</summary>
public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    /// <summary>Topic prefix every subscription must stay under (ADR-004: read-only OT boundary).</summary>
    public const string PlantTopicRoot = "plant/";

    public string MqttHost { get; set; } = "localhost";

    public int MqttPort { get; set; } = 1883;

    /// <summary>MQTT subscription filter. Must start with <c>plant/</c>; the worker never publishes there.</summary>
    public string TopicFilter { get; set; } = "plant/+/+/+/+/+";

    public int HeartbeatSeconds { get; set; } = 30;

    public static bool IsValid(IngestOptions o) =>
        !string.IsNullOrWhiteSpace(o.MqttHost)
        && o.MqttPort is > 0 and <= 65535
        && o.TopicFilter.StartsWith(PlantTopicRoot, StringComparison.Ordinal)
        && o.HeartbeatSeconds is > 0 and <= 3600;
}
