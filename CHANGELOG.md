# Changelog

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
