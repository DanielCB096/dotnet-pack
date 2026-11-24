using PipelineKit.Incremental;
using PipelineKit.Incremental.Sqlite;
using PipelineKit.Quality;
using PipelineKit.Telemetry;

/*
 * Generated PipelineKit job.
 *
 * Flow:
 *   1. Open/create the SQLite checkpoint database.
 *   2. Run one incremental batch with a key watermark.
 *   3. Send invalid rows to quarantine.
 *   4. Emit a PipelineScope for traces and metrics.
 *
 * Replace DemoSource and DemoSink with your real source and destination.
 * For mutable rows, use IncrementalStrategy.TimestampAndKey with an overlap.
 */

var dataDirectory = Environment.GetEnvironmentVariable("PIPELINEKIT_DATA")
    ?? Path.Combine(AppContext.BaseDirectory, "data");

Directory.CreateDirectory(dataDirectory);
var checkpointPath = Path.Combine(dataDirectory, "checkpoints.db");

var store = SqliteCheckpointStore.FromPath(checkpointPath);
await store.EnsureSchemaAsync();

Console.WriteLine($"Checkpoint database: {checkpointPath}");

var runner = new IncrementalRunner(store);
var request = new IncrementalRequest
{
    Pipeline = "demo",
    Source = "local",
    Table = "items",
    Strategy = IncrementalStrategy.Key,
};

using var scope = new PipelineScope("demo", "local", "items", country: "MX");

var result = await runner.RunAsync(request, new DemoSource(), new DemoSink());

scope.RowsExtracted(result.RowsExtracted);
scope.RowsRejected(result.RowsRejected);

Console.WriteLine(
    $"extracted={result.RowsExtracted}, written={result.RowsWritten}, " +
    $"unchanged={result.RowsUnchanged}, rejected={result.RowsRejected}");

// Run the job again: the key checkpoint should prevent re-reading the same rows.
// Replace DemoSource and DemoSink with your real source and destination.
// For mutable rows, switch to TimestampAndKey with an overlap window.

internal sealed class DemoSource : IIncrementalSource
{
    public async IAsyncEnumerable<SourceRow> ReadAsync(
        ExtractionWindow window,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var index = 1; index <= 3; index++)
        {
            if (window.AfterKey is { } afterKey && index <= afterKey)
            {
                continue;
            }

            var modified = start.AddMinutes(index);
            if (window.From is { } from && modified < from)
            {
                continue;
            }

            if (window.To is { } to && modified > to)
            {
                continue;
            }

            var values = new Dictionary<string, object?>
            {
                ["id"] = index,
                ["code"] = index == 2 ? "TOOLONG" : $"C{index}",
            };

            yield return new SourceRow(
                values,
                key: index,
                modifiedAt: modified,
                period: modified.ToString("yyyyMM"));

            await Task.Yield();
        }
    }
}

internal sealed class DemoSink : IBatchSink
{
    private readonly InMemoryQuarantineStore _quarantine;
    private readonly QualityEvaluator _evaluator;

    public DemoSink()
    {
        _quarantine = new InMemoryQuarantineStore();
        _evaluator = new QualityEvaluator(
            [new MaxLengthRule("code", maxLength: 3)],
            _quarantine);
    }

    public async Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default)
    {
        long written = 0;
        long rejected = 0;
        long? lastKey = null;
        DateTimeOffset? lastModified = null;
        string? lastPeriod = null;
        string? lastHash = null;

        await foreach (var row in rows.WithCancellation(ct))
        {
            var quality = await _evaluator.EvaluateAsync(
                row.Values,
                new QuarantineContext(context.Pipeline, context.Source, context.Table, context.BatchId),
                ct);

            if (!quality.IsValid)
            {
                rejected++;
                foreach (var issue in quality.Issues)
                {
                    Console.WriteLine($"quarantine: {issue.Column} -> {issue.Message}");
                }

                continue;
            }

            written++;
            Console.WriteLine($"row {row.Key}: {row.Values["code"]}");
            lastKey = row.Key;
            lastModified = row.ModifiedAt;
            lastPeriod = row.Period;
            lastHash = RowHasher.Compute(row.Values);
        }

        return new BatchWriteResult(
            written,
            rejected,
            lastKey,
            lastModified,
            lastPeriod,
            lastHash);
    }
}
