namespace PipelineKit.Incremental;

/// <summary>Persists and confirms checkpoints.</summary>
public interface ICheckpointStore
{
    /// <summary>Returns the last confirmed checkpoint, or null when the pipeline never ran.</summary>
    Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default);

    /// <summary>
    /// Stores a confirmed checkpoint. It must only be called after the sink
    /// has committed its data.
    /// </summary>
    Task CommitAsync(CheckpointKey key, Checkpoint checkpoint, CancellationToken ct = default);
}

/// <summary>Reads rows from a source for a planned window.</summary>
public interface IIncrementalSource
{
    /// <summary>
    /// Streams rows for the window. Do not materialize the whole source.
    /// </summary>
    IAsyncEnumerable<SourceRow> ReadAsync(ExtractionWindow window, CancellationToken ct = default);
}

/// <summary>Writes a batch. It must consume the whole enumerable or throw.</summary>
public interface IBatchSink
{
    /// <summary>
    /// Writes the rows and returns what was written, rejected or left unchanged.
    /// Throwing prevents the checkpoint from advancing.
    /// </summary>
    Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default);
}
