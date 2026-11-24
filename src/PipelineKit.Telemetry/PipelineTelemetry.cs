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

    public static readonly Counter<long> Batches = Meter.CreateCounter<long>(
        "pipeline.batches",
        description: "Number of pipeline batches started.");

    public static readonly Counter<long> RowsExtracted = Meter.CreateCounter<long>(
        "pipeline.rows.extracted",
        description: "Rows read from the source.");

    public static readonly Counter<long> RowsRejected = Meter.CreateCounter<long>(
        "pipeline.rows.rejected",
        description: "Rows rejected by data quality rules.");

    public static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "pipeline.failures",
        description: "Batches that finished with an error.");

    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "pipeline.duration",
        unit: "ms",
        description: "Batch duration in milliseconds.");
}
