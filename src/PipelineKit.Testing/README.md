# PipelineKit.Testing

Test doubles and dirty-data helpers for PipelineKit.

## Install

```bash
dotnet add package PipelineKit.Testing --version 0.8.0
```

## Included

- `FakeIncrementalSource`: honors the extraction window.
- `CollectingBatchSink`: keeps rows and returns the last position.
- `FailingBatchSink`: fails after N rows to test checkpoint rollback.
- `DirtyDataGenerator`: over-length, null and invalid values.
- `FixedTimeProvider`: deterministic clock.

```csharp
var source = new FakeIncrementalSource(DirtyDataGenerator.Rows(100));
var sink = new CollectingBatchSink();
await runner.RunAsync(request, source, sink);
```

Docs: https://github.com/DanielCB096/dotnet-pack
