using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PipelineKit.Incremental;

/// <summary>
/// Runs one incremental batch and commits the checkpoint only after the sink succeeds.
/// </summary>
public sealed class IncrementalRunner
{
    private readonly ICheckpointStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IncrementalRunner> _logger;

    public IncrementalRunner(
        ICheckpointStore store,
        TimeProvider? timeProvider = null,
        ILogger<IncrementalRunner>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<IncrementalRunner>.Instance;
    }

    public async Task<IncrementalResult> RunAsync(
        IncrementalRequest request,
        IIncrementalSource source,
        IBatchSink sink,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sink);

        var startedAt = _timeProvider.GetUtcNow();
        var key = new CheckpointKey(
            request.Pipeline,
            request.Source,
            request.Table,
            request.Strategy.ToString());

        var checkpoint = await _store.GetAsync(key, ct).ConfigureAwait(false);
        var window = ExtractionWindowPlanner.Plan(request, checkpoint, startedAt);
        var batchId = $"{startedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var context = new BatchContext(
            batchId,
            request.Pipeline,
            request.Source,
            request.Table,
            window,
            startedAt);

        var tracking = new TrackingAsyncEnumerable(source.ReadAsync(window, ct));

        BatchWriteResult writeResult;
        try
        {
            writeResult = await sink
                .WriteAsync(context, tracking.TrackAsync(ct), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch {BatchId} failed; checkpoint was not committed.", batchId);
            throw;
        }

        if (!tracking.Completed)
        {
            throw new InvalidOperationException(
                "The batch sink did not consume the full batch; the checkpoint was not committed.");
        }

        var next = new Checkpoint(
            writeResult.LastKey ?? tracking.LastKey,
            writeResult.LastModified ?? tracking.LastModified,
            writeResult.Period ?? tracking.LastPeriod,
            writeResult.RowHash ?? tracking.LastHash);

        await _store.CommitAsync(key, next, ct).ConfigureAwait(false);

        var duration = _timeProvider.GetUtcNow() - startedAt;
        _logger.LogInformation(
            "Batch {BatchId} committed: {Rows} rows, {Rejected} rejected, {Duration} ms.",
            batchId,
            tracking.Rows,
            writeResult.RowsRejected,
            duration.TotalMilliseconds);

        return new IncrementalResult(
            batchId,
            tracking.Rows,
            writeResult.RowsWritten,
            writeResult.RowsRejected,
            duration,
            CheckpointCommitted: true);
    }

    private sealed class TrackingAsyncEnumerable
    {
        private readonly IAsyncEnumerable<SourceRow> _inner;

        public TrackingAsyncEnumerable(IAsyncEnumerable<SourceRow> inner) => _inner = inner;

        public long Rows { get; private set; }

        public long? LastKey { get; private set; }

        public DateTimeOffset? LastModified { get; private set; }

        public string? LastPeriod { get; private set; }

        public string? LastHash { get; private set; }

        public bool Completed { get; private set; }

        public async IAsyncEnumerable<SourceRow> TrackAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var row in _inner.WithCancellation(ct).ConfigureAwait(false))
            {
                Rows++;
                if (row.Key is { } rowKey && (LastKey is null || rowKey > LastKey))
                {
                    LastKey = rowKey;
                }

                if (row.ModifiedAt is { } modified && (LastModified is null || modified > LastModified))
                {
                    LastModified = modified;
                }

                if (!string.IsNullOrWhiteSpace(row.Period))
                {
                    LastPeriod = row.Period;
                }

                LastHash = RowHasher.Compute(row.Values);
                yield return row;
            }

            Completed = true;
        }
    }
}
