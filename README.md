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
| `PipelineKit.Incremental.Sqlite` | Persistent checkpoints in SQLite | `net10.0` |
| `PipelineKit.Incremental.EntityFrameworkCore` | Checkpoints through EF Core | `net10.0` |
| `PipelineKit.Quality` | Rules, quarantine, schema drift and reconciliation | `net10.0` |
| `PipelineKit.Telemetry` | `ActivitySource` and `Meter` conventions for batches | `net10.0` |
| `PipelineKit.Telemetry.OpenTelemetry` | Resource, tracing/metrics registration and OTLP/console exporters | `net10.0` |
| `PipelineKit.AgentProtocol` | JSON contract and process host for x86 agents | `netstandard2.0`, `net10.0` |
| `PipelineKit.Testing` | Fake source, collecting sink, dirty data and fixed clock | `net10.0` |
| `PipelineKit.Templates` | `dotnet new pipelinekit-job` template | template |

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
src/                  packages
tests/                unit and integration tests
samples/              LegacyBridge control and RabbitMQ consumer
templates/            dotnet new template
docs/                 architecture, code guide and ADRs
docker-compose.yml    RabbitMQ for local end-to-end
```

## Template

```bash
dotnet new install artifacts/PipelineKit.Templates.0.4.0.nupkg
dotnet new pipelinekit-job -n MyPipeline
```

From GitHub Packages, add the feed and install `PipelineKit.Templates::0.4.0`.
See [templates/pipelinekit-job/README.md](templates/pipelinekit-job/README.md).

## Install from GitHub Packages

The `publish` workflow publishes the packages when a `v*` tag is pushed.
See [docs/PUBLISHING.md](docs/PUBLISHING.md) for the feed URL and credentials.

## Messaging and outbox

The LegacyBridge sample writes an outbox event per batch. By default it publishes
to `messages.ndjson`. With RabbitMQ configured, it publishes to a durable topic
exchange and a consumer processes each event exactly once.

```bash
docker compose up -d rabbitmq

RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge/LegacyBridge.Control -c Release -- --reset

RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge.Consumer -c Release -- --once
```

Management UI: http://localhost:15672 (`pipeline` / `pipeline`).

The consumer stores processed message ids in SQLite, so duplicates are acked
without being processed again.

## Documentation

| Document | Content |
|---|---|
| [docs/README.md](docs/README.md) | Documentation index |
| [docs/architecture.md](docs/architecture.md) | Package map, runtime flow, checkpoint semantics, outbox, agent isolation |
| [docs/code-guide.md](docs/code-guide.md) | Reading order, contracts, how to add a source, sink, rule or adapter |
| [docs/PUBLISHING.md](docs/PUBLISHING.md) | Release process, GitHub Packages, template installation |
| [docs/adr](docs/adr/README.md) | Architecture decision records |
| [samples/LegacyBridge/README.md](samples/LegacyBridge/README.md) | End-to-end sample walkthrough |
| [templates/pipelinekit-job/README.md](templates/pipelinekit-job/README.md) | Generated job template |

## Status

Version `0.4.0`. The API may change before `1.0.0`.

Verified locally:

- `dotnet build`: 0 warnings, 0 errors.
- `dotnet test`: **31/31 tests** across 8 test projects.
- `dotnet pack`: 9 packages, including `netstandard2.0` and `net10.0` assets
  for `PipelineKit.AgentProtocol`.
- `LegacyBridge` end-to-end sample runs against a simulated legacy database.
- `LegacyBridge` publishes an outbox event per batch and relays it exactly once.
- RabbitMQ end-to-end job publishes three events and consumes them idempotently.
- `LegacyBridge` is published and executed as `win-x86` in CI.
- `dotnet new pipelinekit-job` generates a runnable project.

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

## End-to-end sample

[`samples/LegacyBridge`](samples/LegacyBridge/README.md) is a complete pipeline:
a simulated legacy ERP, an isolated agent process, incremental extraction,
quarantine, idempotent upserts, hard-delete reconciliation, a transactional
outbox and OpenTelemetry.

```bash
dotnet run --project samples/LegacyBridge/LegacyBridge.Control -c Release -- --reset
```

## License

MIT.
