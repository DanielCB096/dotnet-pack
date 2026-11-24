using System.Collections.Concurrent;

namespace PipelineKit.Incremental;

/// <summary>Thread-safe in-memory checkpoint store, useful for tests and samples.</summary>
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly ConcurrentDictionary<string, Checkpoint> _items = new(StringComparer.Ordinal);

    public Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default)
    {
        _items.TryGetValue(Id(key), out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    public Task CommitAsync(CheckpointKey key, Checkpoint checkpoint, CancellationToken ct = default)
    {
        _items[Id(key)] = checkpoint;
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, Checkpoint> Snapshot => _items;

    private static string Id(CheckpointKey key) =>
        $"{key.Pipeline}|{key.Source}|{key.Table}|{key.Strategy}";
}
