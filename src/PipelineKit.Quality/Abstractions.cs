namespace PipelineKit.Quality;

/// <summary>A rule evaluated against one row.</summary>
public interface IQualityRule
{
    /// <summary>Stable name used in quarantine records and logs.</summary>
    string Name { get; }

    /// <summary>Evaluates one row. Return <see cref="QualityResult.Ok"/> when valid.</summary>
    ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default);
}

/// <summary>Persists rejected values with their evidence.</summary>
public interface IQuarantineStore
{
    /// <summary>Appends one quarantine record.</summary>
    Task AddAsync(QuarantineRecord record, CancellationToken ct = default);
}
