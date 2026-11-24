using System.Globalization;
using System.Text.RegularExpressions;

namespace PipelineKit.Quality;

/// <summary>Fails when a required column is missing, null or whitespace.</summary>
public sealed class NotNullRule(string column) : IQualityRule
{
    public string Name => $"not_null:{column}";

    public ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default)
    {
        if (!row.TryGetValue(column, out var value) || value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValueTask.FromResult(QualityResult.Fail(
                new QualityIssue(Name, column, "Value is required.")));
        }

        return ValueTask.FromResult(QualityResult.Ok);
    }
}

/// <summary>Fails when a value exceeds the declared length.</summary>
public sealed class MaxLengthRule(string column, int maxLength) : IQualityRule
{
    public string Name => $"max_length:{column}:{maxLength}";

    public ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default)
    {
        if (!row.TryGetValue(column, out var value) || value is null)
        {
            return ValueTask.FromResult(QualityResult.Ok);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (text.Length > maxLength)
        {
            return ValueTask.FromResult(QualityResult.Fail(
                new QualityIssue(
                    Name,
                    column,
                    $"Length {text.Length} exceeds the declared maximum of {maxLength}.",
                    text)));
        }

        return ValueTask.FromResult(QualityResult.Ok);
    }
}

/// <summary>Fails when a numeric value is outside the allowed range.</summary>
public sealed class RangeRule(string column, decimal min, decimal max) : IQualityRule
{
    public string Name => $"range:{column}:{min}:{max}";

    public ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default)
    {
        if (!row.TryGetValue(column, out var value) || value is null)
        {
            return ValueTask.FromResult(QualityResult.Ok);
        }

        if (!decimal.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return ValueTask.FromResult(QualityResult.Fail(
                new QualityIssue(Name, column, "Value is not numeric.", value.ToString())));
        }

        if (number < min || number > max)
        {
            return ValueTask.FromResult(QualityResult.Fail(
                new QualityIssue(
                    Name,
                    column,
                    $"Value {number} is outside [{min}, {max}].",
                    value.ToString())));
        }

        return ValueTask.FromResult(QualityResult.Ok);
    }
}

/// <summary>Fails when a string value does not match a regular expression.</summary>
public sealed class RegexRule(string column, string pattern) : IQualityRule
{
    private readonly Regex _regex = new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Name => $"regex:{column}";

    public ValueTask<QualityResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken ct = default)
    {
        if (!row.TryGetValue(column, out var value) || value is null)
        {
            return ValueTask.FromResult(QualityResult.Ok);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (!_regex.IsMatch(text))
        {
            return ValueTask.FromResult(QualityResult.Fail(
                new QualityIssue(Name, column, $"Value does not match {pattern}.", text)));
        }

        return ValueTask.FromResult(QualityResult.Ok);
    }
}
