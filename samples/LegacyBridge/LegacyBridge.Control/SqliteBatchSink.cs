using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PipelineKit.Incremental;
using PipelineKit.Quality;

namespace LegacyBridge.Control;

/// <summary>
/// Writes valid rows to staging and the dimensional table, sends invalid rows
/// to quarantine and records one row per batch.
/// </summary>
public sealed class SqliteBatchSink : IBatchSink
{
    private readonly string _warehousePath;
    private readonly TimeProvider _timeProvider;
    private readonly QualityEvaluator _evaluator;
    private readonly ILogger<SqliteBatchSink> _logger;

    public SqliteBatchSink(
        string warehousePath,
        TimeProvider timeProvider,
        IEnumerable<IQualityRule> rules,
        ILogger<SqliteBatchSink>? logger = null)
    {
        _warehousePath = warehousePath;
        _timeProvider = timeProvider;
        _evaluator = new QualityEvaluator(rules);
        _logger = logger ?? NullLogger<SqliteBatchSink>.Instance;
    }

    public async Task<BatchWriteResult> WriteAsync(
        BatchContext context,
        IAsyncEnumerable<SourceRow> rows,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_warehousePath}");
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();

        long extracted = 0;
        long written = 0;
        long rejected = 0;
        long unchanged = 0;
        long? lastKey = null;
        DateTimeOffset? lastModified = null;
        string? lastPeriod = null;
        string? lastHash = null;
        var now = _timeProvider.GetUtcNow();

        await foreach (var row in rows.WithCancellation(ct).ConfigureAwait(false))
        {
            extracted++;

            var quality = await _evaluator
                .EvaluateAsync(
                    row.Values,
                    new QuarantineContext(
                        context.Pipeline,
                        context.Source,
                        context.Table,
                        context.BatchId,
                        now),
                    ct)
                .ConfigureAwait(false);

            if (!quality.IsValid)
            {
                foreach (var issue in quality.Issues)
                {
                    InsertQuarantine(connection, transaction, context, issue, now);
                }

                rejected++;
                continue;
            }

            var orderId = GetLong(row.Values, "order_id");
            var modified = DateTimeOffset.Parse(
                GetString(row.Values, "updated_at"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            var rowHash = RowHasher.Compute(row.Values);

            var existingHash = GetExistingHash(connection, transaction, orderId);
            if (string.Equals(existingHash, rowHash, StringComparison.Ordinal))
            {
                unchanged++;
            }
            else
            {
                UpsertStaging(connection, transaction, context, row.Values, rowHash, now);
                UpsertDimensional(connection, transaction, context, row.Values, rowHash, now);
                written++;
            }

            if (lastKey is null || orderId > lastKey)
            {
                lastKey = orderId;
            }

            if (lastModified is null || modified > lastModified)
            {
                lastModified = modified;
            }

            lastPeriod = modified.ToString("yyyyMM", CultureInfo.InvariantCulture);
            lastHash = rowHash;
        }

        var finished = _timeProvider.GetUtcNow();
        InsertBatch(connection, transaction, context, extracted, written, rejected, finished);
        InsertOutbox(connection, transaction, context, extracted, written, rejected, finished);

        transaction.Commit();

        _logger.LogInformation(
            "Batch {BatchId}: extracted={Extracted}, written={Written}, rejected={Rejected}",
            context.BatchId,
            extracted,
            written,
            rejected);

        return new BatchWriteResult(
            written,
            rejected,
            lastKey,
            lastModified,
            lastPeriod,
            lastHash,
            unchanged);
    }

    private static string? GetExistingHash(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select row_hash from dw_orders where order_id = $order_id;";
        command.Parameters.AddWithValue("$order_id", orderId);
        return command.ExecuteScalar() as string;
    }

    private static void UpsertStaging(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BatchContext context,
        IReadOnlyDictionary<string, object?> values,
        string rowHash,
        DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into stg_orders
                (order_id, country, customer_id, product_code, quantity, unit_price, status,
                 updated_at, is_deleted, row_hash, batch_id, loaded_at)
            values
                ($order_id, $country, $customer_id, $product_code, $quantity, $unit_price, $status,
                 $updated_at, $is_deleted, $row_hash, $batch_id, $loaded_at)
            on conflict (order_id) do update set
                country = excluded.country,
                customer_id = excluded.customer_id,
                product_code = excluded.product_code,
                quantity = excluded.quantity,
                unit_price = excluded.unit_price,
                status = excluded.status,
                updated_at = excluded.updated_at,
                is_deleted = excluded.is_deleted,
                row_hash = excluded.row_hash,
                batch_id = excluded.batch_id,
                loaded_at = excluded.loaded_at;
            """;
        AddValues(command, values);
        command.Parameters.AddWithValue("$row_hash", rowHash);
        command.Parameters.AddWithValue("$batch_id", context.BatchId);
        command.Parameters.AddWithValue("$loaded_at", now.ToUniversalTime().ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void UpsertDimensional(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BatchContext context,
        IReadOnlyDictionary<string, object?> values,
        string rowHash,
        DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into dw_orders
                (order_id, country, customer_id, product_code, quantity, unit_price, status,
                 updated_at, is_deleted, row_hash, updated_by_batch, updated_at_wh)
            values
                ($order_id, $country, $customer_id, $product_code, $quantity, $unit_price, $status,
                 $updated_at, $is_deleted, $row_hash, $batch_id, $updated_at_wh)
            on conflict (order_id) do update set
                country = excluded.country,
                customer_id = excluded.customer_id,
                product_code = excluded.product_code,
                quantity = excluded.quantity,
                unit_price = excluded.unit_price,
                status = excluded.status,
                updated_at = excluded.updated_at,
                is_deleted = excluded.is_deleted,
                row_hash = excluded.row_hash,
                updated_by_batch = excluded.updated_by_batch,
                updated_at_wh = excluded.updated_at_wh;
            """;
        AddValues(command, values);
        command.Parameters.AddWithValue("$row_hash", rowHash);
        command.Parameters.AddWithValue("$batch_id", context.BatchId);
        command.Parameters.AddWithValue("$updated_at_wh", now.ToUniversalTime().ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void AddValues(
        SqliteCommand command,
        IReadOnlyDictionary<string, object?> values)
    {
        command.Parameters.AddWithValue("$order_id", GetLong(values, "order_id"));
        command.Parameters.AddWithValue("$country", GetString(values, "country"));
        command.Parameters.AddWithValue("$customer_id", GetString(values, "customer_id"));
        command.Parameters.AddWithValue("$product_code", GetString(values, "product_code"));
        command.Parameters.AddWithValue("$quantity", GetInt(values, "quantity"));
        command.Parameters.AddWithValue("$unit_price", GetDouble(values, "unit_price"));
        command.Parameters.AddWithValue("$status", GetString(values, "status"));
        command.Parameters.AddWithValue("$updated_at", GetString(values, "updated_at"));
        command.Parameters.AddWithValue("$is_deleted", GetInt(values, "is_deleted"));
    }

    private static void InsertQuarantine(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BatchContext context,
        QualityIssue issue,
        DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into quarantine
                (pipeline, source, table_name, column_name, raw_value, rule, message, detected_at, batch_id)
            values
                ($pipeline, $source, $table_name, $column_name, $raw_value, $rule, $message, $detected_at, $batch_id);
            """;
        command.Parameters.AddWithValue("$pipeline", context.Pipeline);
        command.Parameters.AddWithValue("$source", context.Source);
        command.Parameters.AddWithValue("$table_name", context.Table);
        command.Parameters.AddWithValue("$column_name", issue.Column);
        command.Parameters.AddWithValue("$raw_value", (object?)issue.RawValue ?? DBNull.Value);
        command.Parameters.AddWithValue("$rule", issue.Rule);
        command.Parameters.AddWithValue("$message", issue.Message);
        command.Parameters.AddWithValue("$detected_at", now.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$batch_id", context.BatchId);
        command.ExecuteNonQuery();
    }

    private static void InsertBatch(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BatchContext context,
        long extracted,
        long written,
        long rejected,
        DateTimeOffset finished)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into batches
                (batch_id, pipeline, source, table_name, rows_extracted, rows_written, rows_rejected, started_at, duration_ms)
            values
                ($batch_id, $pipeline, $source, $table_name, $rows_extracted, $rows_written, $rows_rejected, $started_at, $duration_ms);
            """;
        command.Parameters.AddWithValue("$batch_id", context.BatchId);
        command.Parameters.AddWithValue("$pipeline", context.Pipeline);
        command.Parameters.AddWithValue("$source", context.Source);
        command.Parameters.AddWithValue("$table_name", context.Table);
        command.Parameters.AddWithValue("$rows_extracted", extracted);
        command.Parameters.AddWithValue("$rows_written", written);
        command.Parameters.AddWithValue("$rows_rejected", rejected);
        command.Parameters.AddWithValue("$started_at", context.StartedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$duration_ms", (finished - context.StartedAt).TotalMilliseconds);
        command.ExecuteNonQuery();
    }

    private static void InsertOutbox(
        SqliteConnection connection,
        SqliteTransaction transaction,
        BatchContext context,
        long extracted,
        long written,
        long rejected,
        DateTimeOffset now)
    {
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["batchId"] = context.BatchId,
                ["pipeline"] = context.Pipeline,
                ["source"] = context.Source,
                ["table"] = context.Table,
                ["rowsExtracted"] = extracted,
                ["rowsWritten"] = written,
                ["rowsRejected"] = rejected,
            });

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into outbox (message_id, type, payload, occurred_at)
            values ($message_id, $type, $payload, $occurred_at);
            """;
        command.Parameters.AddWithValue("$message_id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$type", "orders.batch.loaded");
        command.Parameters.AddWithValue("$payload", payload);
        command.Parameters.AddWithValue("$occurred_at", now.ToUniversalTime().ToString("O"));
        command.ExecuteNonQuery();
    }

    private static long GetLong(IReadOnlyDictionary<string, object?> values, string key) =>
        Convert.ToInt64(values[key], CultureInfo.InvariantCulture);

    private static int GetInt(IReadOnlyDictionary<string, object?> values, string key) =>
        Convert.ToInt32(values[key], CultureInfo.InvariantCulture);

    private static double GetDouble(IReadOnlyDictionary<string, object?> values, string key) =>
        Convert.ToDouble(values[key], CultureInfo.InvariantCulture);

    private static string GetString(IReadOnlyDictionary<string, object?> values, string key) =>
        Convert.ToString(values[key], CultureInfo.InvariantCulture) ?? string.Empty;
}
