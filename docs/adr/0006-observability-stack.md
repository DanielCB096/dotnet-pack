# ADR 0006: OpenTelemetry stack for batch pipelines

## Context

API observability is well understood, but batch pipelines have different
questions: how many rows were read, how many were rejected, how old is the
watermark, and which batch failed.

## Decision

- Instrument every batch with an `ActivitySource` named `PipelineKit`.
- Emit counters and a duration histogram with the same meter name.
- Send OTLP to a Collector.
- Route traces to Tempo, metrics to Prometheus and logs to Loki.
- Keep the dashboard in Grafana (`uid: pipelinekit`).
- Never use `order_id`, `customer_id` or similar identifiers as metric labels.

## Consequences

- One trace shows a complete batch.
- Metrics answer volume, quality and latency questions.
- Logs correlate with traces through `trace_id`.
- The Collector must be running for OTLP export; otherwise the sample falls
  back to the console exporter.
