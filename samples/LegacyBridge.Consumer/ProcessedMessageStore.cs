using Microsoft.Data.Sqlite;

namespace LegacyBridge.Consumer;

/// <summary>SQLite store of message ids already processed by the consumer.</summary>
public sealed class ProcessedMessageStore(string databasePath)
{
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table if not exists processed_messages (
                message_id text primary key,
                processed_at text not null
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from processed_messages where message_id = $id;";
        command.Parameters.AddWithValue("$id", messageId);
        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task MarkProcessedAsync(
        string messageId,
        DateTimeOffset processedAt,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            insert or ignore into processed_messages (message_id, processed_at)
            values ($id, $processed_at);
            """;
        command.Parameters.AddWithValue("$id", messageId);
        command.Parameters.AddWithValue("$processed_at", processedAt.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }
}
