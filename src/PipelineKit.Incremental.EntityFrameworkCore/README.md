# PipelineKit.Incremental.EntityFrameworkCore

EF Core checkpoint store for PipelineKit.

## Install

```bash
dotnet add package PipelineKit.Incremental.EntityFrameworkCore --version 0.8.0
```

## Usage

Add the entity to your model:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddPipelineKitCheckpoints();
}
```

Then create the store from an `IDbContextFactory`:

```csharp
var store = new EfCoreCheckpointStore<MyDbContext>(contextFactory);
var runner = new IncrementalRunner(store);
```

`CommitAsync` retries once if a concurrent insert races on the same checkpoint
key.

Docs: https://github.com/DanielCB096/dotnet-pack
