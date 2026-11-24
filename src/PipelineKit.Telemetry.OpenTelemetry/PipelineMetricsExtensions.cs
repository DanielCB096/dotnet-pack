using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using PipelineKit.Telemetry;

namespace PipelineKit.Telemetry.OpenTelemetry;

/// <summary>Registers the PipelineKit meter and its exporters.</summary>
public static class PipelineMetricsExtensions
{
    public static MeterProviderBuilder AddPipelineKitMetrics(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddMeter(PipelineTelemetry.MeterName);
    }

    public static MeterProviderBuilder AddPipelineKitOtlpExporter(
        this MeterProviderBuilder builder,
        Action<OtlpExporterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return configure is null
            ? builder.AddOtlpExporter()
            : builder.AddOtlpExporter(configure);
    }

    public static MeterProviderBuilder AddPipelineKitConsoleExporter(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConsoleExporter();
    }
}
