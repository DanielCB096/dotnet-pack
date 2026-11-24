using System.Globalization;
using System.Text.Json;
using LegacyBridge.Control;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PipelineKit.Incremental;
using PipelineKit.Testing;
using Xunit;

namespace LegacyBridge.Tests;

public sealed class PipelineIntegrationTests : IDisposable
{
    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), $"legacybridge-{Guid.NewGuid():N}");

    [Fact]
    public async Task First_run_loads_valid_rows_and_quarantines_invalid_rows()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new ControlOptions(_dataDirectory, Reset: true);
        var host = new PipelineHost(options, TimeProvider.System, loggerFactory);
        await host.InitializeAsync();

        var legacy = new LegacyDatabase(options.LegacyDbPath);
        var source = new FakeIncrementalSource(ToSourceRows(await legacy.ReadAllAsync()));

        var result = await host.RunAsync(source);

        Assert.Equal(8, result.RowsExtracted);
        Assert.Equal(5, result.RowsWritten);
        Assert.Equal(3, result.RowsRejected);
        Assert.Equal(0, result.RowsUnchanged);

        var report = await host.GetReportAsync();
        Assert.Equal(5, report.DwRows);
        Assert.Equal(3, report.QuarantineRows);
        Assert.Equal(1, report.Checkpoints);
    }

    [Fact]
    public async Task Second_run_only_rereads_the_overlap_boundary_and_marks_it_unchanged()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new ControlOptions(_dataDirectory, Reset: true);
        var host = new PipelineHost(options, TimeProvider.System, loggerFactory);
        await host.InitializeAsync();

        var legacy = new LegacyDatabase(options.LegacyDbPath);
        var source = new FakeIncrementalSource(ToSourceRows(await legacy.ReadAllAsync()));

        await host.RunAsync(source);
        var second = await host.RunAsync(source);

        Assert.Equal(1, second.RowsExtracted);
        Assert.Equal(0, second.RowsWritten);
        Assert.Equal(1, second.RowsUnchanged);
        Assert.Equal(0, second.RowsRejected);
    }

    [Fact]
    public async Task Mutation_updates_new_rows_and_reconciliation_marks_hard_deletes()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new ControlOptions(_dataDirectory, Reset: true);
        var host = new PipelineHost(options, TimeProvider.System, loggerFactory);
        await host.InitializeAsync();

        var legacy = new LegacyDatabase(options.LegacyDbPath);
        await host.RunAsync(new FakeIncrementalSource(ToSourceRows(await legacy.ReadAllAsync())));

        await legacy.MutateAsync();
        var current = await legacy.ReadAllAsync();
        var third = await host.RunAsync(new FakeIncrementalSource(ToSourceRows(current)));

        Assert.Equal(3, third.RowsExtracted);
        Assert.Equal(2, third.RowsWritten);
        Assert.Equal(1, third.RowsUnchanged);

        var keys = current.Select(row => row.OrderId).ToHashSet();
        var reconciled = await host.ReconcileDeletesAsync(keys);

        Assert.Equal(1, reconciled);

        var report = await host.GetReportAsync();
        Assert.Equal(7, report.DwRows);
        Assert.Equal(2, report.DwDeleted);
        Assert.Equal(2, report.Batches);
    }

    [Fact]
    public async Task Outbox_relay_publishes_each_message_once()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new ControlOptions(_dataDirectory, Reset: true);
        var host = new PipelineHost(options, TimeProvider.System, loggerFactory);
        await host.InitializeAsync();

        var legacy = new LegacyDatabase(options.LegacyDbPath);
        await host.RunAsync(new FakeIncrementalSource(ToSourceRows(await legacy.ReadAllAsync())));

        var messagesPath = Path.Combine(_dataDirectory, "messages.ndjson");
        var relay = new OutboxRelay(options.WarehouseDbPath, messagesPath, TimeProvider.System);

        var first = await relay.PublishPendingAsync();
        var second = await relay.PublishPendingAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);

        var lines = await File.ReadAllLinesAsync(messagesPath);
        var line = Assert.Single(lines);
        using var document = JsonDocument.Parse(line);
        Assert.Equal("orders.batch.loaded", document.RootElement.GetProperty("type").GetString());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(_dataDirectory))
            {
                Directory.Delete(_dataDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    private static IReadOnlyList<SourceRow> ToSourceRows(IReadOnlyList<SourceRowData> rows) =>
        rows.Select(row =>
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["order_id"] = row.OrderId,
                ["country"] = row.Country,
                ["customer_id"] = row.CustomerId,
                ["product_code"] = row.ProductCode,
                ["quantity"] = row.Quantity,
                ["unit_price"] = row.UnitPrice,
                ["status"] = row.Status,
                ["updated_at"] = row.UpdatedAt,
                ["is_deleted"] = row.IsDeleted,
            };

            var modified = DateTimeOffset.Parse(
                row.UpdatedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            return new SourceRow(
                values,
                row.OrderId,
                modified,
                modified.ToString("yyyyMM", CultureInfo.InvariantCulture));
        }).ToArray();
}
