# PipelineKit.Telemetry.OpenTelemetry

OpenTelemetry registration and exporters for PipelineKit.

## Install

```bash
dotnet add package PipelineKit.Telemetry.OpenTelemetry --version 0.6.0
```

## Usage

```csharp
var resource = PipelineTelemetryResource.Create("legacybridge", "0.6.0");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resource)
    .AddPipelineKitTracing()
    .AddPipelineKitOtlpExporter()
    .Build();
```

For logs:

```csharp
builder.AddPipelineKitOpenTelemetry();
```

Includes console exporters for local demos and OTLP exporters for a Collector.
Set `OTEL_EXPORTER_OTLP_ENDPOINT` to choose the target.

Docs: https://github.com/DanielCB096/dotnet-pack/blob/main/observability/README.md
