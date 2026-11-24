using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LegacyBridge.Control;

/// <summary>
/// Simulated broker relay. It publishes pending outbox messages to an NDJSON
/// file and marks them as processed. Delivery is at-least-once.
/// </summary>
public sealed class OutboxRelay(string warehousePath, string outputPath, TimeProvider timeProvider)
{
    public async Task<int> PublishPendingAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);

        var pending = new List<PendingMessage>();
        await using (var select = connection.CreateCommand())
        {
            select.CommandText =
                """
                select id, message_id, type, payload, occurred_at
                  from outbox
                 where processed_at is null
                 order by id;
                """;
            await using var reader = await select.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                pending.Add(new PendingMessage(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4)));
            }
        }

        if (pending.Count == 0)
        {
            return 0;
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);

        await using (var writer = new StreamWriter(fullOutputPath, append: true))
        {
            foreach (var message in pending)
            {
                using var payload = JsonDocument.Parse(message.Payload);
                var envelope = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["id"] = message.MessageId,
                    ["type"] = message.Type,
                    ["payload"] = payload.RootElement.Clone(),
                    ["occurredAt"] = message.OccurredAt,
                };

                await writer.WriteLineAsync(JsonSerializer.Serialize(envelope));
            }
        }

        using var transaction = connection.BeginTransaction();
        var now = timeProvider.GetUtcNow().ToUniversalTime().ToString("O");
        foreach (var message in pending)
        {
            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "update outbox set processed_at = $now where id = $id;";
            update.Parameters.AddWithValue("$now", now);
            update.Parameters.AddWithValue("$id", message.Id);
            await update.ExecuteNonQueryAsync(ct);
        }

        transaction.Commit();
        return pending.Count;
    }

    private sealed record PendingMessage(
        long Id,
        string MessageId,
        string Type,
        string Payload,
        string OccurredAt);
}
