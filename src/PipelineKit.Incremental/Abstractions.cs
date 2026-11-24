namespace PipelineKit.Incremental;

/// <summary>Persists and confirms checkpoints.</summary>
public interface ICheckpointStore
{
    Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default);

    Task CommitAsync(CheckpointKey key, Checkpoint checkpoint, CancellationToken ct = default);
}

/// <summary>Reads rows from a source for a planned window.</summary>
public interface IIncrementalSource
{
    IAsyncEnumerable<SourceRow> ReadAsync(ExtractionWindow window, CancellationToken ct = default);
}

/// <summary>Writes a batch. It must consume the whole enumerable or throw.</summary>
public interface IBatchSink
{
    Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default);
}
