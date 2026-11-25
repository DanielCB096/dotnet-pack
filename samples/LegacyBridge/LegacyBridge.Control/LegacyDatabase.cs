using Microsoft.Data.Sqlite;
using PipelineKit.Quality;

namespace LegacyBridge.Control;

/// <summary>Simulated legacy ERP database with dirty data.</summary>
public sealed class LegacyDatabase(string path)
{
    public string Path { get; } = path;

    /// <summary>Creates the legacy schema and seed rows when the database is new.</summary>
    public async Task InitializeAsync(bool reset, CancellationToken ct = default)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        if (reset && File.Exists(Path))
        {
            File.Delete(Path);
        }

        if (File.Exists(Path))
        {
            return;
        }

        await using var connection = new SqliteConnection($"Data Source={Path}");
        await connection.OpenAsync(ct);

        await ExecuteAsync(
            connection,
            """
            create table orders (
                order_id integer primary key,
                country text not null,
                customer_id text not null,
                product_code text not null,
                quantity integer not null,
                unit_price real not null,
                status text not null,
                updated_at text not null,
                is_deleted integer not null default 0
            );
            """,
            ct);

        var rows = new[]
        {
            new SeedRow(1, "MX", "C001", "KEY", 2, 10.0, "confirmed", "2026-01-01T10:00:00Z", 0),
            new SeedRow(2, "MX", "C002", "MOUSE", 1, 25.0, "confirmed", "2026-01-02T10:00:00Z", 0),
            new SeedRow(3, "MX", "C003", "TOOLONG", 1, 5.0, "confirmed", "2026-01-03T10:00:00Z", 0),
            new SeedRow(4, "US", "C004", "PAD", 3, 15.0, "confirmed", "2026-01-04T10:00:00Z", 0),
            new SeedRow(5, "MX", "C005", "KEY", 0, 10.0, "confirmed", "2026-01-05T10:00:00Z", 0),
            new SeedRow(6, "MX", "C006", "MON", 2, 300.0, "cancelled", "2026-01-06T10:00:00Z", 1),
            new SeedRow(7, "CO", "C007", "KEY", 1, 10.0, "confirmed", "2026-01-07T10:00:00Z", 0),
            new SeedRow(8, "MX", "C008", "KEY", 2, 10.0, "confirmed", "2026-01-08T10:00:00Z", 0),
        };

        foreach (var row in rows)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                insert into orders
                    (order_id, country, customer_id, product_code, quantity, unit_price, status, updated_at, is_deleted)
                values
                    ($id, $country, $customer, $code, $quantity, $price, $status, $updated, $deleted);
                """;
            command.Parameters.AddWithValue("$id", row.Id);
            command.Parameters.AddWithValue("$country", row.Country);
            command.Parameters.AddWithValue("$customer", row.Customer);
            command.Parameters.AddWithValue("$code", row.Code);
            command.Parameters.AddWithValue("$quantity", row.Quantity);
            command.Parameters.AddWithValue("$price", row.Price);
            command.Parameters.AddWithValue("$status", row.Status);
            command.Parameters.AddWithValue("$updated", row.UpdatedAt);
            command.Parameters.AddWithValue("$deleted", row.Deleted);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    /// <summary>Simulates a late update, a new order and a hard delete.</summary>
    public async Task MutateAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={Path}");
        await connection.OpenAsync(ct);

        await ExecuteAsync(
            connection,
            """
            update orders
               set product_code = 'PAD',
                   quantity = 2,
                   updated_at = '2026-02-01T09:00:00Z'
             where order_id = 2;

            insert into orders
                (order_id, country, customer_id, product_code, quantity, unit_price, status, updated_at, is_deleted)
            values
                (9, 'MX', 'C009', 'MON', 1, 300.0, 'confirmed', '2026-02-01T09:05:00Z', 0);

            delete from orders where order_id = 7;
            """,
            ct);
    }

    /// <summary>Reads every row; used by tests to build a fake source.</summary>
    public async Task<IReadOnlyList<SourceRowData>> ReadAllAsync(CancellationToken ct = default)
    {
        var rows = new List<SourceRowData>();
        await using var connection = new SqliteConnection($"Data Source={Path}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select order_id, country, customer_id, product_code, quantity, unit_price, status, updated_at, is_deleted
              from orders
             order by order_id;
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new SourceRowData(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetDouble(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetInt32(8)));
        }

        return rows;
    }

    /// <summary>Reads the actual SQLite schema for the preflight check.</summary>
    public async Task<SchemaSnapshot> GetSchemaAsync(CancellationToken ct = default)
    {
        var columns = new List<SchemaColumn>();
        await using var connection = new SqliteConnection($"Data Source={Path}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "pragma table_info(orders);";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var isPrimaryKey = reader.GetInt32(5) != 0;
            var isNotNull = reader.GetInt32(3) != 0 || isPrimaryKey;

            columns.Add(new SchemaColumn(
                reader.GetString(1),
                reader.GetString(2).ToUpperInvariant(),
                MaxLength: null,
                IsNullable: !isNotNull));
        }

        return new SchemaSnapshot(columns);
    }

    /// <summary>Schema the sample expects; drift fails the preflight.</summary>
    public static SchemaSnapshot ExpectedSchema { get; } = new(
    [
        new SchemaColumn("order_id", "INTEGER", IsNullable: false),
        new SchemaColumn("country", "TEXT", IsNullable: false),
        new SchemaColumn("customer_id", "TEXT", IsNullable: false),
        new SchemaColumn("product_code", "TEXT", IsNullable: false),
        new SchemaColumn("quantity", "INTEGER", IsNullable: false),
        new SchemaColumn("unit_price", "REAL", IsNullable: false),
        new SchemaColumn("status", "TEXT", IsNullable: false),
        new SchemaColumn("updated_at", "TEXT", IsNullable: false),
        new SchemaColumn("is_deleted", "INTEGER", IsNullable: false),
    ]);

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record SeedRow(
        long Id,
        string Country,
        string Customer,
        string Code,
        int Quantity,
        double Price,
        string Status,
        string UpdatedAt,
        int Deleted);
}

public sealed record SourceRowData(
    long OrderId,
    string Country,
    string CustomerId,
    string ProductCode,
    int Quantity,
    double UnitPrice,
    string Status,
    string UpdatedAt,
    int IsDeleted);
