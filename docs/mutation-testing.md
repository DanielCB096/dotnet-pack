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

The workflow is a gate: `break` is `80`, so a score below 80 fails the run.
`low` and `high` (85 and 95) only control the report colors.

## Baseline results

Local run on 2026-11-25:

| Config | Score | Killed | Survived |
|---|---:|---:|---:|
| Incremental | **100.00%** | 15 | 0 |
| Quality | **100.00%** | 56 | 0 |

Both configurations kill every executable mutant. Coverage is limited to the
files listed above; these scores are not a whole-project metric.
