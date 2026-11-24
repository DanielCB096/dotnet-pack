using System.Diagnostics;

namespace PipelineKit.Telemetry;

/// <summary>
/// Disposable scope that records one batch as an activity and its metrics.
/// </summary>
public sealed class PipelineScope : IDisposable
{
    private readonly Activity? _activity;
    private readonly KeyValuePair<string, object?>[] _metricTags;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private bool _finished;

    public PipelineScope(
        string pipeline,
        string source,
        string table,
        string? country = null,
        string? batchId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeline);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        _metricTags =
        [
            new("pipeline.name", pipeline),
            new("pipeline.source", source),
            new("pipeline.table", table),
            new("pipeline.country", country),
        ];

        _activity = PipelineTelemetry.ActivitySource.StartActivity("pipeline.batch");
        if (_activity is not null)
        {
            _activity.SetTag("pipeline.name", pipeline);
            _activity.SetTag("pipeline.source", source);
            _activity.SetTag("pipeline.table", table);
            if (country is not null)
            {
                _activity.SetTag("pipeline.country", country);
            }

            if (batchId is not null)
            {
                // Batch identifiers belong in traces/logs, not in metric labels.
                _activity.SetTag("pipeline.batch_id", batchId);
            }
        }

        PipelineTelemetry.Batches.Add(1, _metricTags);
    }

    /// <summary>Marks the current stage, for example extract, validate or load.</summary>
    public void Stage(string stage) => _activity?.SetTag("pipeline.stage", stage);

    /// <summary>Records how many rows were read from the source.</summary>
    public void RowsExtracted(long count)
    {
        if (_activity is not null)
        {
            _activity.SetTag("pipeline.rows.extracted", count);
        }

        PipelineTelemetry.RowsExtracted.Add(count, _metricTags);
    }

    /// <summary>Records how many rows were rejected by quality rules.</summary>
    public void RowsRejected(long count)
    {
        if (_activity is not null)
        {
            _activity.SetTag("pipeline.rows.rejected", count);
        }

        PipelineTelemetry.RowsRejected.Add(count, _metricTags);
    }

    /// <summary>Marks the batch as failed and registers the exception.</summary>
    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        _activity?.AddException(exception);
        PipelineTelemetry.Failures.Add(1, _metricTags);
    }

    /// <summary>Records duration and stops the activity. Safe to call twice.</summary>
    public void Dispose()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        _stopwatch.Stop();
        PipelineTelemetry.Duration.Record(_stopwatch.Elapsed.TotalMilliseconds, _metricTags);
        _activity?.Dispose();
    }
}
