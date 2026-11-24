using System.Collections.Concurrent;

namespace PipelineKit.Incremental;

/// <summary>Thread-safe in-memory checkpoint store, useful for tests and samples.</summary>
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly ConcurrentDictionary<string, Checkpoint> _items = new(StringComparer.Ordinal);

    /// <summary>Returns the stored checkpoint, or null when it does not exist.</summary>
    public Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default)
    {
        _items.TryGetValue(Id(key), out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    /// <summary>Stores or replaces the checkpoint for the key.</summary>
    public Task CommitAsync(CheckpointKey key, Checkpoint checkpoint, CancellationToken ct = default)
    {
        _items[Id(key)] = checkpoint;
        return Task.CompletedTask;
    }

    /// <summary>Snapshot of the current entries, useful in tests.</summary>
    public IReadOnlyDictionary<string, Checkpoint> Snapshot => _items;

    private static string Id(CheckpointKey key) =>
        $"{key.Pipeline}|{key.Source}|{key.Table}|{key.Strategy}";
}
