using Microsoft.Extensions.Options;

namespace SmartFactory.Ingest.Worker;

/// <summary>
/// Week 1 placeholder: proves the worker host, configuration binding and logging work end to end.
/// Week 5 replaces it with the MQTT subscriber → validation → dedup → TimescaleDB pipeline.
/// </summary>
internal sealed partial class HeartbeatWorker(IOptions<IngestOptions> options, ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        LogStarting(logger, o.MqttHost, o.MqttPort, o.TopicFilter);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(o.HeartbeatSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                LogHeartbeat(logger, DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingest worker starting (synthetic data; subscribe-only). MQTT {Host}:{Port}, filter {Filter}")]
    private static partial void LogStarting(ILogger logger, string host, int port, string filter);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ingest worker heartbeat at {Time:O}")]
    private static partial void LogHeartbeat(ILogger logger, DateTimeOffset time);
}
