using PipelineKit.Incremental;
using PipelineKit.Testing;
using Xunit;

namespace PipelineKit.Incremental.Tests;

public sealed class IncrementalRunnerTests
{
    private static IncrementalRequest Request() => new()
    {
        Pipeline = "sales",
        Source = "mx",
        Table = "orders",
        Strategy = IncrementalStrategy.Key,
    };

    [Fact]
    public async Task First_run_extracts_everything_and_commits()
    {
        var store = new InMemoryCheckpointStore();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new IncrementalRunner(store, clock);
        var sink = new CollectingBatchSink();
        var source = new FakeIncrementalSource(DirtyDataGenerator.Rows(3));

        var result = await runner.RunAsync(Request(), source, sink);

        Assert.Equal(3, result.RowsExtracted);
        Assert.Equal(3, result.RowsWritten);
        Assert.True(result.CheckpointCommitted);
        Assert.Equal(3, sink.Rows.Count);

        var checkpoint = await store.GetAsync(
            new CheckpointKey("sales", "mx", "orders", nameof(IncrementalStrategy.Key)));
        Assert.Equal(3, checkpoint!.LastKey);
    }

    [Fact]
    public async Task Second_run_starts_after_the_confirmed_checkpoint()
    {
        var store = new InMemoryCheckpointStore();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new IncrementalRunner(store, clock);
        var source = new FakeIncrementalSource(DirtyDataGenerator.Rows(3));

        await runner.RunAsync(Request(), source, new CollectingBatchSink());
        var second = await runner.RunAsync(Request(), source, new CollectingBatchSink());

        Assert.Equal(0, second.RowsExtracted);
        Assert.Equal(0, second.RowsWritten);
    }

    [Fact]
    public async Task Sink_failure_does_not_advance_the_checkpoint()
    {
        var store = new InMemoryCheckpointStore();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var runner = new IncrementalRunner(store, clock);
        var source = new FakeIncrementalSource(DirtyDataGenerator.Rows(3));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(Request(), source, new FailingBatchSink(failAfterRows: 1)));

        var checkpoint = await store.GetAsync(
            new CheckpointKey("sales", "mx", "orders", nameof(IncrementalStrategy.Key)));
        Assert.Null(checkpoint);
    }

    [Fact]
    public void Row_hash_is_stable_and_key_order_independent()
    {
        var first = new Dictionary<string, object?>
        {
            ["b"] = 2,
            ["a"] = 1,
        };

        var second = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        Assert.Equal(RowHasher.Compute(first), RowHasher.Compute(second));
    }

    [Fact]
    public void Timestamp_window_applies_overlap()
    {
        var checkpoint = new Checkpoint(LastKey: 10, LastModified: new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero));
        var request = new IncrementalRequest
        {
            Pipeline = "sales",
            Source = "mx",
            Table = "orders",
            Strategy = IncrementalStrategy.TimestampAndKey,
            Overlap = TimeSpan.FromMinutes(15),
        };

        var window = ExtractionWindowPlanner.Plan(
            request,
            checkpoint,
            new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 2, 1, 9, 45, 0, TimeSpan.Zero), window.From);
        Assert.Equal(10, window.AfterKey);
    }
}
