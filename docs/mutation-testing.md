# Mutation testing

Stryker.NET mutates the core code and checks whether the tests notice.

## Run

```bash
dotnet tool restore
dotnet stryker --config-file stryker-config.incremental.json
dotnet stryker --config-file stryker-config.quality.json
```

Two configurations are included:

| Config | Project | Mutated files |
|---|---|---|
| `stryker-config.incremental.json` | `PipelineKit.Incremental` | `ExtractionWindowPlanner`, `RowHasher` |
| `stryker-config.quality.json` | `PipelineKit.Quality` | `Rules`, `SchemaDriftDetector`, `Reconciliation` |

Results are written to `StrykerOutput/` (HTML and JSON). The GitHub workflow
`mutation` runs both weekly and on demand.

The `break` threshold is `0` so the workflow reports the score without failing.
Raise it once the suite reaches the desired mutation score.

## Baseline results

Local run on 2026-11-25:

| Config | Score | Killed | Survived |
|---|---:|---:|---:|
| Incremental | **66.67%** | 10 | 3 |
| Quality | **49.30%** | 35 | 12 |

These are baselines, not targets. Add tests for the surviving mutants before
raising the `break` threshold.
