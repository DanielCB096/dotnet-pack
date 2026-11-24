# Code guide

This guide is the recommended reading order and the map of extension points.

## Reading order

1. `src/PipelineKit.Incremental/Models.cs` — the vocabulary: checkpoint, window,
   request, result.
2. `src/PipelineKit.Incremental/Abstractions.cs` — the three contracts:
   checkpoint store, source, sink.
3. `src/PipelineKit.Incremental/ExtractionWindowPlanner.cs` — how the next window
   is calculated.
4. `src/PipelineKit.Incremental/IncrementalRunner.cs` — the commit-after-success
   flow.
5. `src/PipelineKit.Incremental/InMemoryCheckpointStore.cs` — the test store.
6. `src/PipelineKit.Quality/` — rules, quarantine, drift and reconciliation.
7. `src/PipelineKit.Telemetry/` — `PipelineScope` and metric names.
8. `src/PipelineKit.AgentProtocol/` — isolated agent contract and host.
9. `src/PipelineKit.Incremental.Sqlite/` and
   `src/PipelineKit.Incremental.EntityFrameworkCore/` — persistent adapters.
10. `samples/LegacyBridge/` — the end-to-end example.

## The three contracts

### IIncrementalSource

```csharp
public interface IIncrementalSource
{
    IAsyncEnumerable<SourceRow> ReadAsync(
        ExtractionWindow window,
        CancellationToken ct = default);
}
```

The source must respect `window.From`, `window.To`, `window.AfterKey` and
`window.Period`. For `TimestampAndKey`, the timestamp is the primary filter;
applying the key with `AND` would miss late updates.

### IBatchSink

```csharp
public interface IBatchSink
{
    Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default);
}
```

The sink must consume the whole enumerable or throw. It returns the last key,
timestamp and hash it saw, plus written/rejected/unchanged counts.

### ICheckpointStore

```csharp
public interface ICheckpointStore
{
    Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default);
    Task CommitAsync(CheckpointKey key, Checkpoint checkpoint, CancellationToken ct = default);
}
```

The runner commits only after the sink succeeds.

## Minimal pipeline

```csharp
var store = SqliteCheckpointStore.FromPath("data/checkpoints.db");
await store.EnsureSchemaAsync();

var runner = new IncrementalRunner(store);
var request = new IncrementalRequest
{
    Pipeline = "sales",
    Source = "mx",
    Table = "orders",
    Strategy = IncrementalStrategy.Key,
};

var result = await runner.RunAsync(request, new MySource(), new MySink());
```

For mutable rows, use `TimestampAndKey` with an overlap window.

## Adding a source

Implement `IIncrementalSource`. Stream rows; do not load the whole table.

```csharp
public sealed class SqlSource : IIncrementalSource
{
    public async IAsyncEnumerable<SourceRow> ReadAsync(
        ExtractionWindow window,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build a parameterized query from window.From / AfterKey / Period.
        // yield return new SourceRow(values, key, modifiedAt, period);
    }
}
```

## Adding a sink

Implement `IBatchSink` with a transaction:

1. Evaluate quality rules.
2. Write valid rows with an upsert on the natural key.
3. Compare the row hash to skip unchanged rows.
4. Write rejected rows to quarantine.
5. Commit the transaction.
6. Return `BatchWriteResult`.

## Checkpoints

- In-memory: `InMemoryCheckpointStore` (tests, demos).
- SQLite: `PipelineKit.Incremental.Sqlite`.
- EF Core: add `modelBuilder.AddPipelineKitCheckpoints()` and use
  `EfCoreCheckpointStore<TContext>` with an `IDbContextFactory<TContext>`.

## Quality

```csharp
var evaluator = new QualityEvaluator(
    [new NotNullRule("id"), new MaxLengthRule("code", 3)],
    quarantineStore);

var result = await evaluator.EvaluateAsync(row.Values, context);
```

Rules are small classes; add a new one by implementing `IQualityRule`.

## Telemetry

```csharp
using var scope = new PipelineScope("sales", "mx", "orders", country: "MX");
scope.RowsExtracted(rows);
scope.RowsRejected(rejected);
scope.Stage("publish");
```

With OpenTelemetry:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(PipelineTelemetryResource.Create("my-service"))
    .AddPipelineKitTracing()
    .AddPipelineKitOtlpExporter()
    .Build();
```

## Agent protocol

The control writes a request and starts a child process:

```csharp
var host = new AgentProcessHost(executablePath, leadingArguments: "--agent");
var result = await host.RunAsync(request);
```

The agent reads `--request`, writes `--progress` and `--result`, and exits with
a stable code. See `samples/LegacyBridge/LegacyBridge.Control/AgentProgram.cs`.

## Outbox

The LegacyBridge sink writes an event in the same transaction as the batch.
`OutboxRelay` publishes pending events and marks them processed. See
`samples/LegacyBridge/README.md`.

## Template

```bash
dotnet new install artifacts/PipelineKit.Templates.0.4.0.nupkg
dotnet new pipelinekit-job -n MyPipeline
```

The template creates a runnable job with a SQLite checkpoint and a demo
source/sink.

## Common tasks

| Task | Where |
|---|---|
| Add a rule | `src/PipelineKit.Quality/Rules.cs` |
| Add a checkpoint adapter | new project under `src/`, implement `ICheckpointStore` |
| Add an exporter | `src/PipelineKit.Telemetry.OpenTelemetry/` |
| Add a test | `tests/<Package>.Tests/` |
| Add a sample | `samples/` |
| Release | update `Directory.Build.props`, `CHANGELOG.md`, tag `v*` |
