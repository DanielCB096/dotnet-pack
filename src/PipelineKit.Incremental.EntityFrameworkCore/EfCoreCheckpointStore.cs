using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace PipelineKit.Incremental.EntityFrameworkCore;

/// <summary>Stores checkpoints through an EF Core context.</summary>
public sealed class EfCoreCheckpointStore<TContext> : ICheckpointStore
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;

    public EfCoreCheckpointStore(IDbContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>Reads the last confirmed checkpoint, or null.</summary>
    public async Task<Checkpoint?> GetAsync(
        CheckpointKey key,
        CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var record = await db.Set<CheckpointRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(Match(key), ct)
            .ConfigureAwait(false);

        return record is null
            ? null
            : new Checkpoint(
                record.LastKey,
                record.LastModified,
                record.Period,
                record.RowHash);
    }

    /// <summary>Inserts or updates the checkpoint and saves the context.</summary>
    public async Task CommitAsync(
        CheckpointKey key,
        Checkpoint checkpoint,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        await using var db = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var set = db.Set<CheckpointRecord>();
        var record = await set
            .FirstOrDefaultAsync(Match(key), ct)
            .ConfigureAwait(false);

        if (record is null)
        {
            record = new CheckpointRecord
            {
                Pipeline = key.Pipeline,
                Source = key.Source,
                Table = key.Table,
                Strategy = key.Strategy,
            };
            set.Add(record);
        }

        Apply(record, checkpoint);

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Another writer inserted the same checkpoint key between our read
            // and our insert. Re-read and update instead of failing the batch.
            db.ChangeTracker.Clear();
            var existing = await set
                .FirstOrDefaultAsync(Match(key), ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                throw;
            }

            Apply(existing, checkpoint);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private static void Apply(CheckpointRecord record, Checkpoint checkpoint)
    {
        record.LastKey = checkpoint.LastKey;
        record.LastModified = checkpoint.LastModified;
        record.Period = checkpoint.Period;
        record.RowHash = checkpoint.RowHash;
    }

    private static Expression<Func<CheckpointRecord, bool>> Match(CheckpointKey key) =>
        record =>
            record.Pipeline == key.Pipeline &&
            record.Source == key.Source &&
            record.Table == key.Table &&
            record.Strategy == key.Strategy;
}
