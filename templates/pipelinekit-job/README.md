# PipelineKitJob

A PipelineKit incremental job generated with:

```bash
dotnet new pipelinekit-job -n PipelineKitJob
```

## What it does

- Uses a SQLite checkpoint store (`data/checkpoints.db` next to the app).
- Runs an incremental batch with a timestamp watermark and overlap.
- Demonstrates a data quality rule and quarantine.
- Emits a `PipelineScope` for traces and metrics.

Set `PIPELINEKIT_DATA` to choose a different data directory.

Run it:

```bash
dotnet run
```

The second run should extract zero rows because the key checkpoint was committed.
For mutable rows, switch to `IncrementalStrategy.TimestampAndKey` with an
overlap window; see the LegacyBridge sample.

## Next steps

1. Replace `DemoSource` with your real `IIncrementalSource`.
2. Replace `DemoSink` with your real `IBatchSink` (database, file, broker).
3. Add `PipelineKit.Telemetry.OpenTelemetry` and register the exporters.
4. Move the checkpoint database to a durable location.

## Packages

This template references PipelineKit packages from GitHub Packages. Add the
feed to your `NuGet.Config`:

```xml
<packageSources>
  <add key="github" value="https://nuget.pkg.github.com/DanielCB096/index.json" />
</packageSources>
```

See the repository's `docs/PUBLISHING.md` for credentials.
