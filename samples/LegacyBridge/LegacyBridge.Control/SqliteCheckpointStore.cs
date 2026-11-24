using System.Globalization;
using Microsoft.Data.Sqlite;
using PipelineKit.Incremental;

namespace LegacyBridge.Control;

/// <summary>Persists PipelineKit checkpoints in SQLite.</summary>
public sealed class SqliteCheckpointStore(string warehousePath) : ICheckpointStore
{
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table if not exists checkpoints (
                pipeline text not null,
                source text not null,
                table_name text not null,
                strategy text not null,
                last_key integer null,
                last_modified text null,
                period text null,
                row_hash text null,
                primary key (pipeline, source, table_name, strategy)
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Checkpoint?> GetAsync(CheckpointKey key, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select last_key, last_modified, period, row_hash
              from checkpoints
             where pipeline = $pipeline
               and source = $source
               and table_name = $table
               and strategy = $strategy;
            """;
        AddKey(command, key);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new Checkpoint(
            reader.IsDBNull(0) ? null : reader.GetInt64(0),
            reader.IsDBNull(1)
                ? null
                : DateTimeOffset.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    public async Task CommitAsync(
        CheckpointKey key,
        Checkpoint checkpoint,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            insert into checkpoints
                (pipeline, source, table_name, strategy, last_key, last_modified, period, row_hash)
            values
                ($pipeline, $source, $table, $strategy, $last_key, $last_modified, $period, $row_hash)
            on conflict (pipeline, source, table_name, strategy) do update set
                last_key = excluded.last_key,
                last_modified = excluded.last_modified,
                period = excluded.period,
                row_hash = excluded.row_hash;
            """;
        AddKey(command, key);
        command.Parameters.AddWithValue("$last_key", (object?)checkpoint.LastKey ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$last_modified",
            checkpoint.LastModified is { } modified
                ? modified.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : DBNull.Value);
        command.Parameters.AddWithValue("$period", (object?)checkpoint.Period ?? DBNull.Value);
        command.Parameters.AddWithValue("$row_hash", (object?)checkpoint.RowHash ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddKey(SqliteCommand command, CheckpointKey key)
    {
        command.Parameters.AddWithValue("$pipeline", key.Pipeline);
        command.Parameters.AddWithValue("$source", key.Source);
        command.Parameters.AddWithValue("$table", key.Table);
        command.Parameters.AddWithValue("$strategy", key.Strategy);
    }
}
