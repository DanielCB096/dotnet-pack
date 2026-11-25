# Benchmark results

Local run on 2026-11-25.

| Benchmark | Rows | Mean | Allocated |
|---|---:|---:|---:|
| `Hash_10_Fields` | — | **1.038 µs** | 2.38 KB |
| `Run_Batch` | 1,000 | **17.20 ms** | 2.01 MB |
| `Run_Batch` | 10,000 | **178.91 ms** | 20.51 MB |

Environment:

- .NET 10.0.15, SDK 10.0.400
- 13th Gen Intel Core i5-13420H, 12 logical cores
- BenchmarkDotNet 0.15.8, short job (1 warmup, 3 iterations)

These are indicative numbers, not a capacity plan. The runner benchmark includes
the fake source, in-memory checkpoint, collecting sink and row hashing.

Run them again with:

```bash
dotnet run -c Release --project benchmarks/PipelineKit.Benchmarks
```
