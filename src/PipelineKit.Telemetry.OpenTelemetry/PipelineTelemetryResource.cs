using OpenTelemetry.Resources;
using PipelineKit.Telemetry;

namespace PipelineKit.Telemetry.OpenTelemetry;

/// <summary>Builds the resource that identifies a pipeline service.</summary>
public static class PipelineTelemetryResource
{
    public static ResourceBuilder Create(string serviceName, string? serviceVersion = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        return ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion ?? PipelineTelemetry.Version);
    }
}
