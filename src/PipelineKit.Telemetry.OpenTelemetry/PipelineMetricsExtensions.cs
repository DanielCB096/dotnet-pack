using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using PipelineKit.Telemetry;

namespace PipelineKit.Telemetry.OpenTelemetry;

/// <summary>Registers the PipelineKit meter and its exporters.</summary>
public static class PipelineMetricsExtensions
{
    /// <summary>Registers the PipelineKit meter.</summary>
    public static MeterProviderBuilder AddPipelineKitMetrics(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddMeter(PipelineTelemetry.MeterName);
    }

    /// <summary>Adds an OTLP exporter, optionally configured by the caller.</summary>
    public static MeterProviderBuilder AddPipelineKitOtlpExporter(
        this MeterProviderBuilder builder,
        Action<OtlpExporterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return configure is null
            ? builder.AddOtlpExporter()
            : builder.AddOtlpExporter(configure);
    }

    /// <summary>Adds the console exporter for local demos.</summary>
    public static MeterProviderBuilder AddPipelineKitConsoleExporter(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConsoleExporter();
    }
}
