using System.Runtime.CompilerServices;
using PipelineKit.Incremental;

namespace PipelineKit.Testing;

/// <summary>In-memory source that honors the extraction window.</summary>
public sealed class FakeIncrementalSource : IIncrementalSource
{
    private readonly IReadOnlyList<SourceRow> _rows;

    public FakeIncrementalSource(IEnumerable<SourceRow> rows) =>
        _rows = rows?.ToArray() ?? throw new ArgumentNullException(nameof(rows));

    public async IAsyncEnumerable<SourceRow> ReadAsync(
        ExtractionWindow window,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var row in _rows)
        {
            ct.ThrowIfCancellationRequested();

            if (window.AfterKey is { } afterKey && row.Key is { } key && key <= afterKey)
            {
                continue;
            }

            if (window.From is { } from && row.ModifiedAt is { } modified && modified < from)
            {
                continue;
            }

            if (window.To is { } to && row.ModifiedAt is { } modifiedTo && modifiedTo > to)
            {
                continue;
            }

            if (window.Period is { } period && !string.Equals(row.Period, period, StringComparison.Ordinal))
            {
                continue;
            }

            yield return row;
            await Task.Yield();
        }
    }
}
