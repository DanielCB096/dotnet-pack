using PipelineKit.Incremental;

namespace PipelineKit.Testing;

/// <summary>Sink that keeps every row and reports the last position it saw.</summary>
public sealed class CollectingBatchSink : IBatchSink
{
    public List<SourceRow> Rows { get; } = [];

    public BatchContext? LastContext { get; private set; }

    public Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default)
    {
        LastContext = context;
        return CollectAsync(rows, ct);
    }

    private async Task<BatchWriteResult> CollectAsync(
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct)
    {
        long? lastKey = null;
        DateTimeOffset? lastModified = null;
        string? lastPeriod = null;
        string? lastHash = null;
        var written = 0;

        await foreach (var row in rows.WithCancellation(ct).ConfigureAwait(false))
        {
            Rows.Add(row);
            written++;
            if (row.Key is { } key && (lastKey is null || key > lastKey))
            {
                lastKey = key;
            }

            if (row.ModifiedAt is { } modified && (lastModified is null || modified > lastModified))
            {
                lastModified = modified;
            }

            if (!string.IsNullOrWhiteSpace(row.Period))
            {
                lastPeriod = row.Period;
            }

            lastHash = RowHasher.Compute(row.Values);
        }

        return new BatchWriteResult(
            written,
            LastKey: lastKey,
            LastModified: lastModified,
            Period: lastPeriod,
            RowHash: lastHash);
    }
}

/// <summary>Sink that fails after a number of rows, to test checkpoint rollback.</summary>
public sealed class FailingBatchSink(int failAfterRows = 0) : IBatchSink
{
    public async Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default)
    {
        var seen = 0;
        await foreach (var _ in rows.WithCancellation(ct).ConfigureAwait(false))
        {
            seen++;
            if (seen > failAfterRows)
            {
                throw new InvalidOperationException("Simulated sink failure.");
            }
        }

        return new BatchWriteResult(seen);
    }
}
