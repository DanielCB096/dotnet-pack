using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LegacyBridge.Control;

/// <summary>One pending or processed outbox message.</summary>
public sealed record OutboxMessage(
    long Id,
    string MessageId,
    string Type,
    string Payload,
    DateTimeOffset OccurredAt);

/// <summary>Reads pending messages and marks them processed.</summary>
public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken ct = default);

    Task MarkProcessedAsync(string messageId, DateTimeOffset processedAt, CancellationToken ct = default);
}

/// <summary>Publishes one message to a broker or file.</summary>
public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct = default);
}

/// <summary>SQLite outbox store used by the sample.</summary>
public sealed class SqliteOutboxStore(string warehousePath) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int max,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);

        var messages = new List<OutboxMessage>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select id, message_id, type, payload, occurred_at
              from outbox
             where processed_at is null
             order by id
             limit $max;
            """;
        command.Parameters.AddWithValue("$max", max);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            messages.Add(new OutboxMessage(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTimeOffset.Parse(
                    reader.GetString(4),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind)));
        }

        return messages;
    }

    public async Task MarkProcessedAsync(
        string messageId,
        DateTimeOffset processedAt,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={warehousePath}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "update outbox set processed_at = $processed_at where message_id = $message_id;";
        command.Parameters.AddWithValue("$processed_at", processedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$message_id", messageId);
        await command.ExecuteNonQueryAsync(ct);
    }
}

/// <summary>Writes published messages to an NDJSON file, used without a broker.</summary>
public sealed class NdjsonOutboxPublisher(string outputPath) : IOutboxPublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var payload = JsonDocument.Parse(message.Payload);
        var envelope = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = message.MessageId,
            ["type"] = message.Type,
            ["payload"] = payload.RootElement.Clone(),
            ["occurredAt"] = message.OccurredAt,
        };

        await File.AppendAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(envelope) + Environment.NewLine,
            ct);
    }
}

/// <summary>
/// Publishes pending outbox messages and marks them processed. Delivery is
/// at-least-once: if publishing succeeds but marking fails, the message is
/// published again on the next pass.
/// </summary>
public sealed class OutboxRelay(
    IOutboxStore store,
    IOutboxPublisher publisher,
    TimeProvider timeProvider)
{
    public async Task<int> RunOnceAsync(int max = 100, CancellationToken ct = default)
    {
        var pending = await store.GetPendingAsync(max, ct).ConfigureAwait(false);
        foreach (var message in pending)
        {
            await publisher.PublishAsync(message, ct).ConfigureAwait(false);
            await store.MarkProcessedAsync(
                message.MessageId,
                timeProvider.GetUtcNow(),
                ct).ConfigureAwait(false);
        }

        return pending.Count;
    }
}
