using System.Text.RegularExpressions;

namespace SmartFactory.Platform.ArchitectureTests;

/// <summary>
/// ADR-004, the read-only OT boundary: only the plant simulator publishes on <c>plant/#</c>. Every other deployable
/// may subscribe, never publish. This scans the source of all four deployables and the web app, because the
/// rule crosses the language boundary (C#, Python, TypeScript) and no single assembly check could cover it.
/// </summary>
public sealed partial class OtBoundaryTests
{
    // Source that is allowed to publish telemetry: the simulator, and nothing else.
    private static readonly string[] AllowedPublisherRoots = ["src/simulator/"];

    private static readonly string[] ScannedExtensions = [".cs", ".py", ".ts"];

    [Fact]
    public void Only_the_simulator_publishes_on_plant_topics()
    {
        var root = RepositoryRoot();
        var offenders = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.*", SearchOption.AllDirectories)
            .Where(f => ScannedExtensions.Contains(Path.GetExtension(f)))
            .Select(f => (Full: f, Relative: Path.GetRelativePath(root, f).Replace('\\', '/')))
            .Where(f => !IsGeneratedOrDependency(f.Relative))
            .Where(f => !AllowedPublisherRoots.Any(allowed => f.Relative.StartsWith(allowed, StringComparison.Ordinal)))
            .Where(f => f.Relative != "src/platform-api/tests/SmartFactory.Platform.ArchitectureTests/OtBoundaryTests.cs")
            .Where(f => PublishesMqtt(File.ReadAllText(f.Full)))
            .Select(f => f.Relative)
            .ToList();

        offenders.Should().BeEmpty("only the simulator may publish on plant/# (ADR-004); the platform is advisory and read-only");
    }

    [Theory]
    [InlineData("await mqttClient.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(\"plant/x\").Build());", true)]
    [InlineData("client.publish(\"plant/tnt/plant-01/line-a/MTR-042/temperature\", payload, qos=1)", true)]
    [InlineData("await client.publish(topic, payload)  # aiomqtt", true)]
    [InlineData("await mqttClient.SubscribeAsync(\"plant/#\");", false)]
    [InlineData("await outbox.PublishAsync(alertRaised);  // Kafka, not MQTT", false)]
    public void The_detector_recognises_mqtt_publishing(string source, bool expected)
    {
        var withLibrary = (source.Contains("Mqtt", StringComparison.Ordinal) || source.Contains("client.publish", StringComparison.Ordinal))
            ? "using MQTTnet;\nimport paho.mqtt.client as mqtt\n" + source
            : source;
        PublishesMqtt(withLibrary).Should().Be(expected);
    }

    /// <summary>An MQTT client library is in use and something is published through it.</summary>
    private static bool PublishesMqtt(string source) =>
        MqttLibrary().IsMatch(source) && (MqttPublishCall().IsMatch(source) || PlantTopicPublish().IsMatch(source));

    private static bool IsGeneratedOrDependency(string relative) =>
        relative.Contains("/bin/", StringComparison.Ordinal) || relative.Contains("/obj/", StringComparison.Ordinal)
        || relative.Contains("/node_modules/", StringComparison.Ordinal) || relative.Contains("/.venv/", StringComparison.Ordinal)
        || relative.Contains("/dist/", StringComparison.Ordinal) || relative.Contains("/.angular/", StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SmartFactory.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root (SmartFactory.slnx) not found.");
    }

    [GeneratedRegex(@"MQTTnet|paho\.mqtt|aiomqtt|asyncio_mqtt|gmqtt|from\s+['""]mqtt['""]|require\(['""]mqtt['""]\)", RegexOptions.IgnoreCase)]
    private static partial Regex MqttLibrary();

    [GeneratedRegex(@"MqttApplicationMessage|mqtt\w*\.PublishAsync\s*\(|\bclient\.publish\s*\(|\.publish\s*\(\s*['""]plant/", RegexOptions.IgnoreCase)]
    private static partial Regex MqttPublishCall();

    [GeneratedRegex(@"publish\w*\s*\(\s*(f?['""]|\$"")plant/", RegexOptions.IgnoreCase)]
    private static partial Regex PlantTopicPublish();
}
