namespace PipelineKit.Quality;

/// <summary>A rule evaluated against one row.</summary>
public interface IQualityRule
{
    string Name { get; }

    ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default);
}

/// <summary>Persists rejected values with their evidence.</summary>
public interface IQuarantineStore
{
    Task AddAsync(QuarantineRecord record, CancellationToken ct = default);
}
