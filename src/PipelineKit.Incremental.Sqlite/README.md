# PipelineKit.Incremental.Sqlite

SQLite checkpoint store for PipelineKit.

## Install

```bash
dotnet add package PipelineKit.Incremental.Sqlite --version 0.6.0
```

## Usage

```csharp
var store = SqliteCheckpointStore.FromPath("data/checkpoints.db");
await store.EnsureSchemaAsync();

var runner = new IncrementalRunner(store);
```

The store creates a `checkpoints` table keyed by pipeline, source, table and
strategy. It is a good fit for a single-process job, a desktop app or a demo.

Docs: https://github.com/DanielCB096/dotnet-pack
