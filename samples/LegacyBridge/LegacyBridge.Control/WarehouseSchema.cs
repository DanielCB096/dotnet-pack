using Microsoft.Data.Sqlite;

namespace LegacyBridge.Control;

/// <summary>Creates the staging, dimensional, quarantine and control tables.</summary>
public static class WarehouseSchema
{
    public static async Task EnsureAsync(string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table if not exists stg_orders (
                order_id integer primary key,
                country text not null,
                customer_id text not null,
                product_code text not null,
                quantity integer not null,
                unit_price real not null,
                status text not null,
                updated_at text not null,
                is_deleted integer not null,
                row_hash text not null,
                batch_id text not null,
                loaded_at text not null
            );

            create table if not exists dw_orders (
                order_id integer primary key,
                country text not null,
                customer_id text not null,
                product_code text not null,
                quantity integer not null,
                unit_price real not null,
                status text not null,
                updated_at text not null,
                is_deleted integer not null,
                row_hash text not null,
                updated_by_batch text not null,
                updated_at_wh text not null
            );

            create table if not exists quarantine (
                id integer primary key autoincrement,
                pipeline text not null,
                source text not null,
                table_name text not null,
                column_name text not null,
                raw_value text null,
                rule text not null,
                message text not null,
                detected_at text not null,
                batch_id text null
            );

            create table if not exists batches (
                batch_id text primary key,
                pipeline text not null,
                source text not null,
                table_name text not null,
                rows_extracted integer not null,
                rows_written integer not null,
                rows_rejected integer not null,
                started_at text not null,
                duration_ms real not null
            );

            create table if not exists outbox (
                id integer primary key autoincrement,
                message_id text not null unique,
                type text not null,
                payload text not null,
                occurred_at text not null,
                processed_at text null
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }
}
