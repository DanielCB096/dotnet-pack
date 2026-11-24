using Microsoft.Data.Sqlite;
using PipelineKit.Incremental;
using PipelineKit.Incremental.Sqlite;
using Xunit;

namespace PipelineKit.Incremental.Sqlite.Tests;

public sealed class SqliteCheckpointStoreTests : IDisposable
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"pipelinekit-{Guid.NewGuid():N}.db");

    private SqliteCheckpointStore CreateStore() =>
        SqliteCheckpointStore.FromPath(_databasePath);

    private static CheckpointKey Key() =>
        new("sales", "mx", "orders", "TimestampAndKey");

    [Fact]
    public async Task Missing_checkpoint_returns_null()
    {
        var store = CreateStore();
        await store.EnsureSchemaAsync();

        var checkpoint = await store.GetAsync(Key());

        Assert.Null(checkpoint);
    }

    [Fact]
    public async Task Commit_and_read_roundtrip_preserves_the_timestamp()
    {
        var store = CreateStore();
        await store.EnsureSchemaAsync();

        var modified = new DateTimeOffset(2026, 2, 1, 9, 30, 0, TimeSpan.Zero);
        await store.CommitAsync(Key(), new Checkpoint(42, modified, "202602", "abc"));

        var checkpoint = await store.GetAsync(Key());

        Assert.NotNull(checkpoint);
        Assert.Equal(42, checkpoint!.LastKey);
        Assert.Equal(modified, checkpoint.LastModified);
        Assert.Equal("202602", checkpoint.Period);
        Assert.Equal("abc", checkpoint.RowHash);
    }

    [Fact]
    public async Task Commit_overwrites_the_existing_checkpoint()
    {
        var store = CreateStore();
        await store.EnsureSchemaAsync();

        await store.CommitAsync(Key(), new Checkpoint(1));
        await store.CommitAsync(Key(), new Checkpoint(2));

        var checkpoint = await store.GetAsync(Key());

        Assert.Equal(2, checkpoint!.LastKey);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
        }
    }
}
