using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PipelineKit.Telemetry;
using PipelineKit.Telemetry.OpenTelemetry;
using Xunit;

namespace PipelineKit.Telemetry.OpenTelemetry.Tests;

public sealed class PipelineTelemetryExtensionsTests
{
    [Fact]
    public void Resource_contains_the_service_name()
    {
        var resource = PipelineTelemetryResource.Create("legacybridge", "1.2.3").Build();
        var attributes = resource.Attributes.ToDictionary(a => a.Key, a => a.Value);

        Assert.Equal("legacybridge", attributes["service.name"]);
        Assert.Equal("1.2.3", attributes["service.version"]);
    }

    [Fact]
    public void Tracing_registration_builds_a_provider_and_records_an_activity()
    {
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == PipelineTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured = activity,
        };

        ActivitySource.AddActivityListener(listener);

        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddPipelineKitTracing()
            .Build();

        using (var scope = new PipelineScope("sales", "mx", "orders"))
        {
            scope.RowsExtracted(3);
        }

        Assert.NotNull(provider);
        Assert.NotNull(captured);
        Assert.Equal("pipeline.batch", captured!.OperationName);
    }

    [Fact]
    public void Console_exporter_builds_without_throwing()
    {
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddPipelineKitTracing()
            .AddPipelineKitConsoleExporter()
            .Build();

        Assert.NotNull(provider);
    }
}
