namespace PipelineKit.Quality;

/// <summary>Evaluates a row against multiple rules and optionally quarantines failures.</summary>
public sealed class QualityEvaluator
{
    private readonly IReadOnlyList<IQualityRule> _rules;
    private readonly IQuarantineStore? _quarantine;

    public QualityEvaluator(
        IEnumerable<IQualityRule> rules,
        IQuarantineStore? quarantine = null)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _quarantine = quarantine;
    }

    /// <summary>
    /// Runs every rule. Invalid rows are optionally written to the quarantine
    /// store once per issue.
    /// </summary>
    public async Task<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        QuarantineContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(context);

        var issues = new List<QualityIssue>();
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(row, ct).ConfigureAwait(false);
            if (!result.IsValid)
            {
                issues.AddRange(result.Issues);
            }
        }

        if (issues.Count == 0)
        {
            return QualityResult.Ok;
        }

        if (_quarantine is not null)
        {
            var detectedAt = context.DetectedAt ?? DateTimeOffset.UtcNow;
            foreach (var issue in issues)
            {
                await _quarantine.AddAsync(
                    new QuarantineRecord(
                        context.Pipeline,
                        context.Source,
                        context.Table,
                        issue.Column,
                        issue.RawValue,
                        issue.Rule,
                        issue.Message,
                        detectedAt,
                        context.BatchId),
                    ct).ConfigureAwait(false);
            }
        }

        return QualityResult.Fail(issues);
    }
}
