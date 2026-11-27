using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SmartFactory.BuildingBlocks.Telemetry;

/// <summary>Binds the <c>Telemetry</c> configuration section.</summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; set; } = "smartfactory";

    /// <summary>OTLP/gRPC endpoint. Empty disables export (traces still flow in-process).</summary>
    public string OtlpEndpoint { get; set; } = string.Empty;
}

public static class TelemetryExtensions
{
    /// <summary>
    /// OpenTelemetry traces and metrics with W3C trace context (ADR-003 envelope <c>trace_id</c>).
    /// Locally nothing is exported unless <c>Telemetry:OtlpEndpoint</c> is set; in Azure the endpoint is
    /// the Monitor OTLP ingestion.
    /// </summary>
    public static IServiceCollection AddPlatformTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(options.ServiceName, serviceNamespace: "smartfactory")
                .AddAttributes([new KeyValuePair<string, object>("smartfactory.data", "synthetic")]));

        otel.WithTracing(t =>
        {
            t.AddAspNetCoreInstrumentation(o => o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health"));
            t.AddHttpClientInstrumentation();
            if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            {
                t.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            }
        });

        otel.WithMetrics(m =>
        {
            m.AddAspNetCoreInstrumentation();
            m.AddHttpClientInstrumentation();
            m.AddRuntimeInstrumentationIfAvailable();
            if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            {
                m.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            }
        });

        return services;
    }

    // Why: runtime instrumentation is a separate package added in Week 10; this keeps the call site stable.
    private static MeterProviderBuilder AddRuntimeInstrumentationIfAvailable(this MeterProviderBuilder builder) => builder;
}
