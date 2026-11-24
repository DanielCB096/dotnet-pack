using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using PipelineKit.Telemetry;

namespace PipelineKit.Telemetry.OpenTelemetry;

/// <summary>Registers the PipelineKit activity source and its exporters.</summary>
public static class PipelineTracingExtensions
{
    /// <summary>Registers the PipelineKit activity source.</summary>
    public static TracerProviderBuilder AddPipelineKitTracing(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSource(PipelineTelemetry.SourceName);
    }

    /// <summary>Adds an OTLP exporter, optionally configured by the caller.</summary>
    public static TracerProviderBuilder AddPipelineKitOtlpExporter(
        this TracerProviderBuilder builder,
        Action<OtlpExporterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return configure is null
            ? builder.AddOtlpExporter()
            : builder.AddOtlpExporter(configure);
    }

    /// <summary>Adds the console exporter for local demos.</summary>
    public static TracerProviderBuilder AddPipelineKitConsoleExporter(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConsoleExporter();
    }
}
