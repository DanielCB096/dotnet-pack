namespace PipelineKit.Quality;

/// <summary>One quality problem found in a row.</summary>
public sealed record QualityIssue(
    string Rule,
    string Column,
    string Message,
    string? RawValue = null);

/// <summary>Result of evaluating a row against a set of rules.</summary>
public sealed record QualityResult
{
    public static QualityResult Ok { get; } = new(Array.Empty<QualityIssue>());

    public QualityResult(IReadOnlyList<QualityIssue> issues) => Issues = issues;

    public IReadOnlyList<QualityIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public static QualityResult Fail(params QualityIssue[] issues) => new(issues);

    public static QualityResult Fail(IReadOnlyList<QualityIssue> issues) => new(issues);
}

/// <summary>Problematic row preserved for later inspection and reprocessing.</summary>
public sealed record QuarantineRecord(
    string Pipeline,
    string Source,
    string Table,
    string Column,
    string? RawValue,
    string Rule,
    string Message,
    DateTimeOffset DetectedAt,
    string? BatchId = null);

/// <summary>Context needed to write quarantine records.</summary>
public sealed record QuarantineContext(
    string Pipeline,
    string Source,
    string Table,
    string? BatchId = null,
    DateTimeOffset? DetectedAt = null);

/// <summary>A column in a source schema.</summary>
public sealed record SchemaColumn(
    string Name,
    string DataType,
    int? MaxLength = null,
    bool IsNullable = true);

/// <summary>Snapshot of a table schema.</summary>
public sealed record SchemaSnapshot(IReadOnlyList<SchemaColumn> Columns)
{
    public IReadOnlyDictionary<string, SchemaColumn> ByName =>
        Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
}

public enum SchemaChangeKind
{
    Added,
    Removed,
    TypeChanged,
    LengthChanged,
    NullabilityChanged,
}

public sealed record SchemaDriftEntry(
    SchemaChangeKind Kind,
    string Column,
    string? Expected,
    string? Actual);

public sealed record SchemaDrift(IReadOnlyList<SchemaDriftEntry> Changes)
{
    public bool HasChanges => Changes.Count > 0;
}

/// <summary>Result of comparing source and target aggregates.</summary>
public sealed record ReconciliationResult(
    bool IsMatch,
    long? SourceCount,
    long? TargetCount,
    decimal? SourceSum,
    decimal? TargetSum,
    string? Details = null);
