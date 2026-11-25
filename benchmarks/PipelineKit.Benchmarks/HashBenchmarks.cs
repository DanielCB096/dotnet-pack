using BenchmarkDotNet.Attributes;
using PipelineKit.Incremental;

namespace PipelineKit.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class HashBenchmarks
{
    private readonly Dictionary<string, object?> _row = new()
    {
        ["order_id"] = 123456,
        ["country"] = "MX",
        ["customer_id"] = "C-00042",
        ["product_code"] = "KEY",
        ["quantity"] = 2,
        ["unit_price"] = 10.5,
        ["status"] = "confirmed",
        ["updated_at"] = "2026-11-25T10:00:00Z",
        ["is_deleted"] = 0,
        ["notes"] = "sample row",
    };

    [Benchmark]
    public string Hash_10_Fields() => RowHasher.Compute(_row);
}
