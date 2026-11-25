using BenchmarkDotNet.Attributes;
using PipelineKit.Incremental;
using PipelineKit.Testing;

namespace PipelineKit.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class RunnerBenchmarks
{
    private FakeIncrementalSource _source = null!;
    private IncrementalRequest _request = null!;
    private IncrementalRunner _runner = null!;

    [Params(1_000, 10_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _source = new FakeIncrementalSource(DirtyDataGenerator.Rows(Rows));
        _request = new IncrementalRequest
        {
            Pipeline = "bench",
            Source = "local",
            Table = "rows",
            Strategy = IncrementalStrategy.Key,
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _runner = new IncrementalRunner(new InMemoryCheckpointStore());
    }

    [Benchmark]
    public Task<IncrementalResult> Run_Batch() =>
        _runner.RunAsync(_request, _source, new CollectingBatchSink());
}
