# PipelineKit.Incremental

Incremental extraction primitives for .NET: checkpoints, watermarks, overlap
windows, row hashes and commit-after-success.

## Install

```bash
dotnet add package PipelineKit.Incremental --version 0.6.0
```

## What it solves

- A confirmed checkpoint that never advances before the sink succeeds.
- Windows by timestamp, key, period (`AAAAMM`) or full snapshot.
- Overlap so late updates are re-read.
- Stable row hashes to skip unchanged rows.
- At-least-once delivery with idempotent sinks.

## Minimal usage

```csharp
var runner = new IncrementalRunner(store);
var result = await runner.RunAsync(request, source, sink);
```

Implement `IIncrementalSource` and `IBatchSink`. The checkpoint is committed only
after the sink consumes the full batch.

Docs: https://github.com/DanielCB096/dotnet-pack/blob/main/docs/incremental-runner.md
