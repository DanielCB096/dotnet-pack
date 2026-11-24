namespace PipelineKit.Incremental;

/// <summary>Strategy used to plan the next extraction window.</summary>
public enum IncrementalStrategy
{
    /// <summary>Modified timestamp plus a stable key.</summary>
    TimestampAndKey,

    /// <summary>Monotonic numeric key.</summary>
    Key,

    /// <summary>Period identifier, for example AAAAMM.</summary>
    Period,

    /// <summary>Full snapshot; the source decides what to return.</summary>
    Snapshot,
}

/// <summary>Identifies an independent checkpoint.</summary>
public sealed record CheckpointKey(string Pipeline, string Source, string Table, string Strategy);

/// <summary>Last confirmed position for one pipeline, source and table.</summary>
public sealed record Checkpoint(
    long? LastKey = null,
    DateTimeOffset? LastModified = null,
    string? Period = null,
    string? RowHash = null);

/// <summary>Window requested from the source.</summary>
public sealed record ExtractionWindow(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    long? AfterKey = null,
    string? Period = null,
    TimeSpan Overlap = default);

/// <summary>Request to run one incremental batch.</summary>
public sealed record IncrementalRequest
{
    public required string Pipeline { get; init; }

    public required string Source { get; init; }

    public required string Table { get; init; }

    public IncrementalStrategy Strategy { get; init; } = IncrementalStrategy.TimestampAndKey;

    /// <summary>How far back to re-read to catch late updates.</summary>
    public TimeSpan Overlap { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>First period to read when there is no checkpoint yet.</summary>
    public string? InitialPeriod { get; init; }
}

/// <summary>Metadata for one execution.</summary>
public sealed record BatchContext(
    string BatchId,
    string Pipeline,
    string Source,
    string Table,
    ExtractionWindow Window,
    DateTimeOffset StartedAt);

/// <summary>What the sink wrote.</summary>
public sealed record BatchWriteResult(
    long RowsWritten,
    long RowsRejected = 0,
    long? LastKey = null,
    DateTimeOffset? LastModified = null,
    string? Period = null,
    string? RowHash = null);

/// <summary>Outcome of an incremental run.</summary>
public sealed record IncrementalResult(
    string BatchId,
    long RowsExtracted,
    long RowsWritten,
    long RowsRejected,
    TimeSpan Duration,
    bool CheckpointCommitted);

/// <summary>One row read from the source.</summary>
public sealed class SourceRow
{
    public SourceRow(
        IReadOnlyDictionary<string, object?> values,
        long? key = null,
        DateTimeOffset? modifiedAt = null,
        string? period = null)
    {
        Values = values;
        Key = key;
        ModifiedAt = modifiedAt;
        Period = period;
    }

    public IReadOnlyDictionary<string, object?> Values { get; }

    public long? Key { get; }

    public DateTimeOffset? ModifiedAt { get; }

    public string? Period { get; }
}
