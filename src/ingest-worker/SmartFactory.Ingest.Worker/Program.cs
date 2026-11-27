using SmartFactory.BuildingBlocks.Telemetry;
using SmartFactory.Ingest.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<IngestOptions>()
    .Bind(builder.Configuration.GetSection(IngestOptions.SectionName))
    .Validate(IngestOptions.IsValid, "Ingest options are invalid: MqttHost, MqttPort (1-65535) and a plant/ TopicFilter are required.")
    .ValidateOnStart();

builder.Services.AddPlatformTelemetry(builder.Configuration);
builder.Services.AddHostedService<HeartbeatWorker>();

var host = builder.Build();
host.Run();
