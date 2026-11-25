# Changelog

## [0.6.0] - 2026-11-25

### Added

- Per-package README files packed into every NuGet package.
- SourceLink, symbol packages (`.snupkg`) and a package icon.
- Coverlet coverage, ReportGenerator summary and coverage artifact in CI.
- `dotnet format --verify-no-changes` and vulnerable-package checks in CI.
- Dependabot, CONTRIBUTING, SECURITY, issue templates, PR template and CODEOWNERS.
- ADRs for the outbox, Trusted Publishing and the observability stack.
- Streaming NDJSON in `AgentIncrementalSource`.
- EF Core checkpoint commit retry for concurrent writers, with a test.
- LegacyBridge schema preflight and a schema-drift test.
- Dashboard screenshot in the README and observability guide.
- NuGet.org install commands for all packages and the template.

## [0.5.0] - 2026-11-25

### Added

- RabbitMQ outbox publisher and idempotent `LegacyBridge.Consumer`.
- `docker-compose.yml` with a durable RabbitMQ broker.
- RabbitMQ end-to-end CI job and tests for publish failure and inbox idempotency.
- Observability stack (Collector, Tempo, Prometheus, Loki, Grafana) and the
  `PipelineKit pipelines` dashboard.
- `AddPipelineKitOpenTelemetry` logging helper.
- BenchmarkDotNet suites and benchmark results.
- Architecture and code-guide documentation.
- LegacyBridge integration tests and Windows `win-x86` CI job.

## [0.4.0] - 2026-11-24

### Added

- `PipelineKit.Incremental.EntityFrameworkCore`: EF Core checkpoint store and
  model-builder extension, with tests.
- LegacyBridge outbox: every batch writes an event in the same transaction and
  a relay publishes it to an NDJSON file. Second relay pass publishes zero.
- CI generates, restores and runs the `dotnet new pipelinekit-job` template.

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
