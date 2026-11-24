using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PipelineKit.Incremental;
using PipelineKit.Quality;
using PipelineKit.Telemetry;

namespace LegacyBridge.Control;

public sealed class PipelineHost
{
    private static readonly IncrementalRequest Request = new()
    {
        Pipeline = "sales",
        Source = "legacy",
        Table = "orders",
        Strategy = IncrementalStrategy.TimestampAndKey,
        Overlap = TimeSpan.FromMinutes(5),
    };

    private readonly ControlOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PipelineHost> _logger;
    private readonly ILogger<SqliteBatchSink> _sinkLogger;
    private readonly ILogger<IncrementalRunner> _runnerLogger;
    private readonly SqliteCheckpointStore _checkpoints;

    public PipelineHost(
        ControlOptions options,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<PipelineHost>();
        _sinkLogger = loggerFactory.CreateLogger<SqliteBatchSink>();
        _runnerLogger = loggerFactory.CreateLogger<IncrementalRunner>();
        _checkpoints = new SqliteCheckpointStore(options.WarehouseDbPath);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_options.Reset)
        {
            TryDelete(_options.WarehouseDbPath);
        }

        await new LegacyDatabase(_options.LegacyDbPath).InitializeAsync(_options.Reset, ct);
        await WarehouseSchema.EnsureAsync(_options.WarehouseDbPath, ct);
        await _checkpoints.EnsureSchemaAsync(ct);
    }

    public async Task<IncrementalResult> RunAsync(CancellationToken ct = default)
    {
        var source = new AgentIncrementalSource(
            _options.LegacyDbPath,
            ResolveAgentExecutable());

        var sink = new SqliteBatchSink(
            _options.WarehouseDbPath,
            _timeProvider,
            Rules(),
            _sinkLogger);

        var runner = new IncrementalRunner(_checkpoints, _timeProvider, _runnerLogger);

        using var scope = new PipelineScope(
            Request.Pipeline,
            Request.Source,
            Request.Table,
            country: "MX");

        try
        {
            var result = await runner.RunAsync(Request, source, sink, ct);
            scope.Stage("publish");
            scope.RowsExtracted(result.RowsExtracted);
            scope.RowsRejected(result.RowsRejected);
            _logger.LogInformation(
                "Pipeline finished: extracted={Extracted}, written={Written}, rejected={Rejected}",
                result.RowsExtracted,
                result.RowsWritten,
                result.RowsRejected);
            return result;
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    /// <summary>
    /// Hard deletes never appear in a timestamp window. This pass compares keys
    /// present in the source with the dimensional table and marks missing rows.
    /// </summary>
    public async Task<int> ReconcileDeletesAsync(CancellationToken ct = default)
    {
        var source = new AgentIncrementalSource(
            _options.LegacyDbPath,
            ResolveAgentExecutable());

        var legacyKeys = await source.ReadKeysAsync(ct);

        await using var connection = new SqliteConnection($"Data Source={_options.WarehouseDbPath}");
        await connection.OpenAsync(ct);

        var missing = new List<long>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select order_id from dw_orders where is_deleted = 0;";
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var orderId = reader.GetInt64(0);
                if (!legacyKeys.Contains(orderId))
                {
                    missing.Add(orderId);
                }
            }
        }

        var now = _timeProvider.GetUtcNow().ToUniversalTime().ToString("O");
        foreach (var orderId in missing)
        {
            await using var update = connection.CreateCommand();
            update.CommandText =
                """
                update dw_orders
                   set is_deleted = 1,
                       updated_at_wh = $now
                 where order_id = $order_id;
                """;
            update.Parameters.AddWithValue("$now", now);
            update.Parameters.AddWithValue("$order_id", orderId);
            await update.ExecuteNonQueryAsync(ct);
        }

        return missing.Count;
    }

    public async Task<LegacyBridgeReport> GetReportAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_options.WarehouseDbPath}");
        await connection.OpenAsync(ct);

        return new LegacyBridgeReport(
            DwRows: await ScalarAsync(connection, "select count(*) from dw_orders;", ct),
            DwDeleted: await ScalarAsync(connection, "select count(*) from dw_orders where is_deleted = 1;", ct),
            QuarantineRows: await ScalarAsync(connection, "select count(*) from quarantine;", ct),
            Batches: await ScalarAsync(connection, "select count(*) from batches;", ct),
            Checkpoints: await ScalarAsync(connection, "select count(*) from checkpoints;", ct));
    }

    private static IQualityRule[] Rules() =>
    [
        new NotNullRule("order_id"),
        new MaxLengthRule("product_code", maxLength: 3),
        new RangeRule("quantity", min: 1, max: 1000),
    ];

    private static string ResolveAgentExecutable() =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot determine the current executable path.");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }

    private static async Task<long> ScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}

public sealed record LegacyBridgeReport(
    long DwRows,
    long DwDeleted,
    long QuarantineRows,
    long Batches,
    long Checkpoints);
