using Microsoft.Extensions.Configuration;

namespace SmartFactory.Ingest.Worker.Tests;

public sealed class IngestOptionsTests
{
    [Fact]
    public void Defaults_are_valid_and_subscribe_only_under_plant()
    {
        var options = new IngestOptions();

        IngestOptions.IsValid(options).Should().BeTrue();
        options.TopicFilter.Should().StartWith(IngestOptions.PlantTopicRoot);
    }

    [Theory]
    [InlineData("", 1883, "plant/+/+/+/+/+", 30)]
    [InlineData("localhost", 0, "plant/+/+/+/+/+", 30)]
    [InlineData("localhost", 70000, "plant/+/+/+/+/+", 30)]
    [InlineData("localhost", 1883, "#", 30)]
    [InlineData("localhost", 1883, "control/plant/#", 30)]
    [InlineData("localhost", 1883, "plant/#", 0)]
    public void Rejects_hosts_ports_filters_outside_the_ot_boundary_and_bad_intervals(string host, int port, string filter, int heartbeat)
    {
        var options = new IngestOptions { MqttHost = host, MqttPort = port, TopicFilter = filter, HeartbeatSeconds = heartbeat };

        IngestOptions.IsValid(options).Should().BeFalse();
    }

    [Fact]
    public void Binds_from_configuration_section()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ingest:MqttHost"] = "mosquitto",
                ["Ingest:MqttPort"] = "1883",
                ["Ingest:TopicFilter"] = "plant/tnt-alsafa/#",
                ["Ingest:HeartbeatSeconds"] = "5",
            })
            .Build();

        var options = configuration.GetSection(IngestOptions.SectionName).Get<IngestOptions>();

        options.Should().NotBeNull();
        options.MqttHost.Should().Be("mosquitto");
        options.TopicFilter.Should().Be("plant/tnt-alsafa/#");
        options.HeartbeatSeconds.Should().Be(5);
        IngestOptions.IsValid(options).Should().BeTrue();
    }
}
