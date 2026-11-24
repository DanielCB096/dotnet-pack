using System.Diagnostics;
using PipelineKit.Telemetry;
using Xunit;

namespace PipelineKit.Telemetry.Tests;

public sealed class PipelineScopeTests
{
    [Fact]
    public void Scope_records_an_activity_with_batch_tags()
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

        using (var scope = new PipelineScope(
                   pipeline: "sales",
                   source: "mx",
                   table: "orders",
                   country: "MX",
                   batchId: "b-1"))
        {
            scope.Stage("extract");
            scope.RowsExtracted(10);
            scope.RowsRejected(2);
        }

        Assert.NotNull(captured);
        Assert.Equal("pipeline.batch", captured!.OperationName);
        Assert.Equal("sales", captured.GetTagItem("pipeline.name"));
        Assert.Equal("MX", captured.GetTagItem("pipeline.country"));
        Assert.Equal("b-1", captured.GetTagItem("pipeline.batch_id"));
        Assert.Equal(10L, captured.GetTagItem("pipeline.rows.extracted"));
        Assert.Equal(ActivityStatusCode.Unset, captured.Status);
    }

    [Fact]
    public void Failure_marks_the_activity_as_error()
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

        using (var scope = new PipelineScope("sales", "mx", "orders"))
        {
            scope.Fail(new InvalidOperationException("boom"));
        }

        Assert.NotNull(captured);
        Assert.Equal(ActivityStatusCode.Error, captured!.Status);
    }
}
