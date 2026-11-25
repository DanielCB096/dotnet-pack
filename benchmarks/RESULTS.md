# Benchmark results

Local run on 2026-11-25.

| Benchmark | Rows | Mean | Allocated |
|---|---:|---:|---:|
| `Hash_10_Fields` | — | **2.067 µs** | 2.38 KB |
| `Run_Batch` | 1,000 | **3.642 ms** | 2.00 MB |
| `Run_Batch` | 10,000 | **41.16 ms** | 20.36 MB |

Environment:

- .NET 11.0.0, SDK 11.0.100
- 13th Gen Intel Core i5-13420H, 12 logical cores
- BenchmarkDotNet 0.16.0-preview.2, short job (1 warmup, 3 iterations)

These are indicative numbers, not a capacity plan. The runner benchmark includes
the fake source, in-memory checkpoint, collecting sink and row hashing.

Run them again with:

```bash
dotnet run -c Release --project benchmarks/PipelineKit.Benchmarks
```
