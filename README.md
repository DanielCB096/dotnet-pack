# PipelineKit

Reusable .NET building blocks for **incremental, observable and testable data pipelines**.

PipelineKit is not an ETL framework. It is a small set of packages that solve the
parts that usually get copied and pasted between projects:

- confirmed checkpoints and overlap windows,
- idempotent batch commits,
- data quality rules and quarantine,
- schema drift detection and reconciliation,
- OpenTelemetry-friendly pipeline telemetry,
- a versioned contract for isolated x86 extraction agents.

The core packages do not depend on a database, a broker or a cloud provider.
Adapters can be added without dragging infrastructure into your domain.

## Packages

| Package | What it does | Target |
|---|---|---|
| `PipelineKit.Incremental` | Checkpoints, watermarks, overlap windows and commit-after-success | `net10.0` |
| `PipelineKit.Quality` | Rules, quarantine, schema drift and reconciliation | `net10.0` |
| `PipelineKit.Telemetry` | `ActivitySource` and `Meter` conventions for batches | `net10.0` |
| `PipelineKit.AgentProtocol` | JSON contract and process host for x86 agents | `netstandard2.0`, `net10.0` |
| `PipelineKit.Testing` | Fake source, collecting sink, dirty data and fixed clock | `net10.0` |

`AgentProtocol` targets `netstandard2.0` so the same contract can be used by a
modern x64 orchestrator and by a .NET Framework 4.8 x86 agent when the legacy
driver forces it.

## Quick start

```bash
dotnet restore PipelineKit.slnx --configfile NuGet.Config
dotnet build PipelineKit.slnx -c Release
dotnet test PipelineKit.slnx -c Release
dotnet run --project samples/PipelineKit.Sample.LegacyBridge -c Release
dotnet pack PipelineKit.slnx -c Release -o artifacts
```

## Example

```csharp
var store = new InMemoryCheckpointStore();
var runner = new IncrementalRunner(store);
var request = new IncrementalRequest
{
    Pipeline = "sales",
    Source = "mx",
    Table = "orders",
    Strategy = IncrementalStrategy.TimestampAndKey,
    Overlap = TimeSpan.FromMinutes(30),
};

var result = await runner.RunAsync(request, source, sink);

Console.WriteLine($"{result.RowsWritten} rows committed in {result.Duration}");
```

The checkpoint is committed **only after the sink succeeds**. If the batch fails,
the next run re-reads the same window and the sink must be idempotent.

## Design principles

1. **No infrastructure in the core.** Databases, brokers and cloud SDKs live in adapters.
2. **Commit after success.** A checkpoint never advances before the data is safe.
3. **At-least-once delivery.** Idempotency belongs to the sink, not to hope.
4. **Small APIs.** Request objects instead of parameter lists.
5. **Streaming by default.** No full materialization of large sources.
6. **Telemetry as a first-class citizen.** Traces and metrics use stable names.
7. **Tests are part of the product.** Every package has unit tests; helpers are published.

## Repository layout

```
src/        packages
tests/      unit tests
samples/    runnable end-to-end sample
docs/adr/   architecture decisions
```

## Status

Version `0.1.0`. The API may change before `1.0.0`.

Verified locally:

- `dotnet build`: 0 warnings, 0 errors.
- `dotnet test`: **15/15 tests** across 4 test projects.
- `dotnet pack`: 5 packages, including `netstandard2.0` and `net10.0` assets
  for `PipelineKit.AgentProtocol`.

Before publishing, update `PackageProjectUrl` and `RepositoryUrl` in
`Directory.Build.props` with your GitHub repository.

## Sample output

```text
First run : extracted=5, written=5, batch=20260201080000-...
Second run: extracted=0 (checkpoint works)
Quarantined row 3: Length 12 exceeds the declared maximum of 3.
Quarantine records: 1
Schema drift: 3 change(s)
  - TypeChanged: id (int -> bigint)
  - LengthChanged: code (3 -> 9)
  - Added: segment ( -> varchar)
Reconciliation: match=True
```

## License

MIT.
