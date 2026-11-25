# Benchmarks

BenchmarkDotNet benchmarks for the incremental runner and the row hash.

## Run

```bash
dotnet run -c Release --project benchmarks/PipelineKit.Benchmarks
```

Filter one suite:

```bash
dotnet run -c Release --project benchmarks/PipelineKit.Benchmarks -- --filter *HashBenchmarks*
```

Results are written to `BenchmarkDotNet.Artifacts/` (ignored by git).

See [RESULTS.md](RESULTS.md) for the latest local run.

## What is measured

- `HashBenchmarks.Hash_10_Fields`: SHA-256 over a 10-field row.
- `RunnerBenchmarks.Run_Batch`: full runner with a fake source, in-memory
  checkpoint and collecting sink, for 1,000 and 10,000 rows.
