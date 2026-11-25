using PipelineKit.Incremental;
using Xunit;

namespace PipelineKit.Incremental.Tests;

public sealed class ExtractionWindowPlannerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Key_strategy_uses_the_after_key()
    {
        var request = Request(IncrementalStrategy.Key);

        var window = ExtractionWindowPlanner.Plan(
            request,
            new Checkpoint(LastKey: 42),
            Now);

        Assert.Equal(42, window.AfterKey);
        Assert.Null(window.From);
        Assert.Null(window.To);
        Assert.Null(window.Period);
    }

    [Fact]
    public void Period_strategy_uses_the_checkpoint_or_initial_period()
    {
        var request = Request(IncrementalStrategy.Period) with { InitialPeriod = "202601" };

        var initial = ExtractionWindowPlanner.Plan(request, null, Now);
        var next = ExtractionWindowPlanner.Plan(
            request,
            new Checkpoint(Period: "202602"),
            Now);

        Assert.Equal("202601", initial.Period);
        Assert.Equal("202602", next.Period);
    }

    [Fact]
    public void Snapshot_strategy_has_no_filters()
    {
        var request = Request(IncrementalStrategy.Snapshot);

        var window = ExtractionWindowPlanner.Plan(
            request,
            new Checkpoint(LastKey: 10, LastModified: Now, Period: "202601"),
            Now);

        Assert.Null(window.From);
        Assert.Null(window.To);
        Assert.Null(window.AfterKey);
        Assert.Null(window.Period);
    }

    [Fact]
    public void Timestamp_strategy_sets_to_now_and_keeps_the_key()
    {
        var request = Request(IncrementalStrategy.TimestampAndKey) with
        {
            Overlap = TimeSpan.FromMinutes(10),
        };

        var window = ExtractionWindowPlanner.Plan(
            request,
            new Checkpoint(LastKey: 10, LastModified: Now.AddHours(-1)),
            Now);

        Assert.Equal(Now.AddHours(-1).Subtract(TimeSpan.FromMinutes(10)), window.From);
        Assert.Equal(Now, window.To);
        Assert.Equal(10, window.AfterKey);
    }

    private static IncrementalRequest Request(IncrementalStrategy strategy) => new()
    {
        Pipeline = "sales",
        Source = "mx",
        Table = "orders",
        Strategy = strategy,
    };
}
