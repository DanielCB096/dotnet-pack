using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PipelineKit.Incremental;
using PipelineKit.Incremental.EntityFrameworkCore;
using Xunit;

namespace PipelineKit.Incremental.EntityFrameworkCore.Tests;

public sealed class EfCoreCheckpointStoreTests : IDisposable
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"pipelinekit-ef-{Guid.NewGuid():N}.db");

    private static CheckpointKey Key() =>
        new("sales", "mx", "orders", "TimestampAndKey");

    [Fact]
    public async Task Missing_checkpoint_returns_null()
    {
        var store = await CreateStoreAsync();

        var checkpoint = await store.GetAsync(Key());

        Assert.Null(checkpoint);
    }

    [Fact]
    public async Task Commit_and_read_roundtrip_preserves_the_timestamp()
    {
        var store = await CreateStoreAsync();
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
        var store = await CreateStoreAsync();

        await store.CommitAsync(Key(), new Checkpoint(1));
        await store.CommitAsync(Key(), new Checkpoint(2));

        var checkpoint = await store.GetAsync(Key());

        Assert.Equal(2, checkpoint!.LastKey);
    }

    [Fact]
    public async Task Concurrent_commits_do_not_throw()
    {
        var store = await CreateStoreAsync();

        await Task.WhenAll(
            store.CommitAsync(Key(), new Checkpoint(1)),
            store.CommitAsync(Key(), new Checkpoint(2)));

        var checkpoint = await store.GetAsync(Key());

        Assert.NotNull(checkpoint);
        Assert.Contains(checkpoint!.LastKey, new long?[] { 1, 2 });
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

    private async Task<EfCoreCheckpointStore<TestDbContext>> CreateStoreAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        await using (var db = new TestDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        return new EfCoreCheckpointStore<TestDbContext>(new TestDbContextFactory(options));
    }
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.AddPipelineKitCheckpoints();
}

public sealed class TestDbContextFactory(DbContextOptions<TestDbContext> options)
    : IDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext() => new(options);
}
