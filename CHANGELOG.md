# Changelog

## [0.4.0] - 2026-11-24

### Added

- `PipelineKit.Incremental.EntityFrameworkCore`: EF Core checkpoint store and
  model-builder extension, with tests.
- LegacyBridge outbox: every batch writes an event in the same transaction and
  a relay publishes it to an NDJSON file. Second relay pass publishes zero.
- CI generates, restores and runs the `dotnet new pipelinekit-job` template.

## [Unreleased]

### Added

- Integration tests for the LegacyBridge pipeline: initial load, overlap
  idempotency, mutation and hard-delete reconciliation.
- Windows CI job that publishes and runs the sample as `win-x86`.
- RabbitMQ outbox publisher and idempotent `LegacyBridge.Consumer`.
- `docker-compose.yml` with a durable RabbitMQ broker.
- RabbitMQ end-to-end CI job.
- Tests for publish failure and consumer inbox idempotency.

## [0.3.0] - 2026-11-24

### Added

- `PipelineKit.Incremental.Sqlite`: SQLite checkpoint store, moved out of the
  LegacyBridge sample and published as a package.
- `PipelineKit.Telemetry.OpenTelemetry`: resource helper, tracing/metrics
  registration and configurable OTLP/console exporters.
- `PipelineKit.Templates`: `dotnet new pipelinekit-job` template.
- Tests for both new packages.

## [0.2.0] - 2026-11-24

### Added

- `samples/LegacyBridge`: end-to-end sample with a simulated legacy database,
  isolated agent process, incremental extraction, quarantine, idempotent upserts,
  hard-delete reconciliation and OpenTelemetry console exporter.
- `BatchWriteResult.RowsUnchanged` and `IncrementalResult.RowsUnchanged` to
  distinguish re-read rows from real changes.
- `AgentProcessHost` accepts leading arguments, so a single executable can run
  both control and agent modes.

### Fixed

- A late update with a lower key no longer regresses the checkpoint.
- Timestamp windows use the timestamp as the primary filter; applying the key
  with `AND` missed late updates.
- `FakeIncrementalSource` follows the same timestamp/key semantics.

## [0.1.0] - 2026-11-24

### Added

- `PipelineKit.Incremental`: checkpoints, extraction windows, overlap, row hashing
  and commit-after-success.
- `PipelineKit.Quality`: data quality rules, quarantine, schema drift detection
  and reconciliation.
- `PipelineKit.Telemetry`: `ActivitySource` and `Meter` conventions for batch
  pipelines.
- `PipelineKit.AgentProtocol`: versioned JSON contract and process host for
  isolated x86 agents. Targets `netstandard2.0` and `net10.0`.
- `PipelineKit.Testing`: fake source, collecting sink, dirty data generator and
  fixed clock.
- End-to-end sample: `samples/PipelineKit.Sample.LegacyBridge`.
- Unit tests: 15 tests across four test projects.
- CI workflow for build, test and pack.
- Publish workflow for GitHub Packages and GitHub Releases.
- Architecture decision records for checkpoint commit, agent isolation and
  infrastructure-free core packages.
