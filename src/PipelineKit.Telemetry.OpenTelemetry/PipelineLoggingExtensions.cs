using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace PipelineKit.Telemetry.OpenTelemetry;

/// <summary>Registers the OpenTelemetry logging provider with an OTLP exporter.</summary>
public static class PipelineLoggingExtensions
{
    public static ILoggingBuilder AddPipelineKitOpenTelemetry(
        this ILoggingBuilder builder,
        Action<OpenTelemetryLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddOtlpExporter();
            configure?.Invoke(options);
        });

        return builder;
    }
}
