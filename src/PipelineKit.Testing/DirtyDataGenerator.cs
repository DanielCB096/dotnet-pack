using PipelineKit.Incremental;

namespace PipelineKit.Testing;

/// <summary>Generates rows with realistic problems for quarantine and drift tests.</summary>
public static class DirtyDataGenerator
{
    /// <summary>Returns a value longer than the declared column length.</summary>
    public static IReadOnlyDictionary<string, object?> OverLength(
        string column,
        int declaredLength,
        int extra = 5) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [column] = new string('X', declaredLength + extra),
        };

    /// <summary>Returns a row with a null value.</summary>
    public static IReadOnlyDictionary<string, object?> MissingValue(string column) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [column] = null,
        };

    /// <summary>Returns a row with a negative amount.</summary>
    public static IReadOnlyDictionary<string, object?> NegativeAmount(string column) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [column] = -1,
        };

    /// <summary>Builds a deterministic set of rows with sequential keys.</summary>
    public static IReadOnlyList<SourceRow> Rows(
        int count,
        DateTimeOffset? start = null,
        Func<int, IReadOnlyDictionary<string, object?>>? valueFactory = null)
    {
        var from = start ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rows = new List<SourceRow>(count);

        for (var index = 1; index <= count; index++)
        {
            var values = valueFactory?.Invoke(index)
                         ?? new Dictionary<string, object?>(StringComparer.Ordinal)
                         {
                             ["id"] = index,
                             ["name"] = $"row-{index}",
                         };

            rows.Add(new SourceRow(
                values,
                key: index,
                modifiedAt: from.AddMinutes(index),
                period: from.AddMinutes(index).ToString("yyyyMM")));
        }

        return rows;
    }
}
