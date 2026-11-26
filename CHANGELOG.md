# Changelog

## [Unreleased]

### Added

- Handover guide (`docs/handover.md`) with infrastructure, release runbook and
  first-week checklist.
- 1.0 roadmap (`docs/roadmap-1.0.md`) with API-freeze workstreams, open
  decisions and release checklist.
- ADR 0007 documenting the .NET 11 baseline.
- Repository rename plan (`docs/rename-repo.md`) with impact, runbook and rollback.

### Changed

- Refreshed the README test and coverage badges.
- `scripts/publish-nuget.ps1` reads the version from `Directory.Build.props`
  instead of a hard-coded default.

## [0.8.0] - 2026-11-25

### Changed

- Migrated all projects to `net11.0` and .NET SDK `11.0.100`.
- Updated EF Core, `Microsoft.Extensions.*`, `Microsoft.Data.Sqlite`,
  `System.Text.Json` and `Microsoft.SourceLink.GitHub` to the 11.0 wave.
- `PipelineKit.AgentProtocol` now targets `netstandard2.0` and `net11.0`.
- The `pipelinekit-job` template now generates `net11.0` projects.
- CI, packaging and documentation use the .NET 11 SDK.

### Breaking

- Packages no longer include `net10.0` assets. Applications must target
  `net11.0` or later to consume this release.

## [0.7.0] - 2026-11-25

### Added

- Stryker.NET mutation testing for Incremental and Quality, with `mutation` workflow.
- DocFX documentation site deployed to GitHub Pages.
- End-to-end observability CI (`observability` workflow) that runs the stack,
  the sample and asserts Prometheus, Tempo and Loki data.
- Demo storyboard and extra tests for hashing and extraction windows.
- Mutation score raised to 100% in both configurations; `break` is now 80, so
  the `mutation` workflow fails on a regression.
- Simplified `MaxLengthRule` and `RangeRule` to avoid redundant missing/null
  branches, with contract tests for rule names, messages and boundaries.

### Changed

- GitHub Actions updated: `checkout` v7, `setup-dotnet` v6, `upload-artifact` v7
  and `gh-release` v3.
- `xunit.runner.visualstudio` updated to 4.0.1.

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
