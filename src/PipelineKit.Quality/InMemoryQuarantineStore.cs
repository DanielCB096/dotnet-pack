using System.Collections.Concurrent;

namespace PipelineKit.Quality;

/// <summary>Thread-safe in-memory quarantine store for tests and samples.</summary>
public sealed class InMemoryQuarantineStore : IQuarantineStore
{
    private readonly ConcurrentQueue<QuarantineRecord> _items = new();

    public Task AddAsync(QuarantineRecord record, CancellationToken ct = default)
    {
        _items.Enqueue(record);
        return Task.CompletedTask;
    }

    public IReadOnlyList<QuarantineRecord> Items => _items.ToArray();
}
