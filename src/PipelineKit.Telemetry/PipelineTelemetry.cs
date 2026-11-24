using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PipelineKit.Telemetry;

/// <summary>
/// Shared ActivitySource and Meter for pipelines. Register the source and meter
/// names in the OpenTelemetry SDK to export traces and metrics.
/// </summary>
public static class PipelineTelemetry
{
    public const string SourceName = "PipelineKit";
    public const string MeterName = SourceName;
    public const string Version = "0.1.0";

    public static readonly ActivitySource ActivitySource = new(SourceName, Version);
    public static readonly Meter Meter = new(SourceName, Version);

    /// <summary>Number of batches started.</summary>
    public static readonly Counter<long> Batches = Meter.CreateCounter<long>(
        "pipeline.batches",
        description: "Number of pipeline batches started.");

    /// <summary>Rows read from the source.</summary>
    public static readonly Counter<long> RowsExtracted = Meter.CreateCounter<long>(
        "pipeline.rows.extracted",
        description: "Rows read from the source.");

    /// <summary>Rows rejected by quality rules.</summary>
    public static readonly Counter<long> RowsRejected = Meter.CreateCounter<long>(
        "pipeline.rows.rejected",
        description: "Rows rejected by data quality rules.");

    /// <summary>Batches that finished with an error.</summary>
    public static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "pipeline.failures",
        description: "Batches that finished with an error.");

    /// <summary>Batch duration in milliseconds.</summary>
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "pipeline.duration",
        unit: "ms",
        description: "Batch duration in milliseconds.");
}
