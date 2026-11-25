# PipelineKit.Telemetry

OpenTelemetry-friendly `ActivitySource` and `Meter` conventions for batch
pipelines.

## Install

```bash
dotnet add package PipelineKit.Telemetry --version 0.8.0
```

## Usage

```csharp
using var scope = new PipelineScope("sales", "mx", "orders", country: "MX");
scope.RowsExtracted(rows);
scope.RowsRejected(rejected);
scope.Stage("publish");
```

Names:

- Activity: `pipeline.batch`
- Source/Meter: `PipelineKit`
- Counters: `pipeline.batches`, `pipeline.rows.extracted`,
  `pipeline.rows.rejected`, `pipeline.failures`
- Histogram: `pipeline.duration` (ms)

Use `PipelineKit.Telemetry.OpenTelemetry` to register the source and meter.

Docs: https://github.com/DanielCB096/dotnet-pack
