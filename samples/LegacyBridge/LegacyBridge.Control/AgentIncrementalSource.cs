using System.Globalization;
using System.Reflection;
using System.Text.Json;
using PipelineKit.AgentProtocol;
using PipelineKit.Incremental;

namespace LegacyBridge.Control;

/// <summary>
/// PipelineKit source that runs the extraction in a separate agent process.
/// The sample materializes the batch; a production source should stream it.
/// </summary>
public sealed class AgentIncrementalSource(string legacyDbPath, string agentExecutable)
    : IIncrementalSource
{
    public async IAsyncEnumerable<SourceRow> ReadAsync(
        ExtractionWindow window,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var rows = await QueryAsync(BuildQuery(window), ct).ConfigureAwait(false);

        foreach (var values in rows)
        {
            ct.ThrowIfCancellationRequested();
            yield return ToSourceRow(values);
        }
    }

    public async Task<IReadOnlySet<long>> ReadKeysAsync(CancellationToken ct = default)
    {
        var rows = await QueryAsync("select order_id from orders order by order_id;", ct)
            .ConfigureAwait(false);

        return rows
            .Select(values => Convert.ToInt64(values["order_id"], CultureInfo.InvariantCulture))
            .ToHashSet();
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(
        string query,
        CancellationToken ct)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "legacybridge");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"{Guid.NewGuid():N}.ndjson");

        try
        {
            var request = new AgentRequest(
                AgentId: "legacy-agent",
                SourceId: legacyDbPath,
                Table: "orders",
                Query: query,
                OutputPath: outputPath);

            var host = CreateHost();
            var result = await host.RunAsync(request, ct).ConfigureAwait(false);

            if (result.ExitCode != AgentExitCodes.Success)
            {
                throw new InvalidOperationException(
                    $"The legacy agent failed with exit code {result.ExitCode}: {result.Error}");
            }

            var rows = new List<Dictionary<string, object?>>();
            foreach (var line in File.ReadLines(outputPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                var values = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    values[property.Name] = ToClr(property.Value);
                }

                rows.Add(values);
            }

            return rows;
        }
        finally
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    private AgentProcessHost CreateHost()
    {
        var processPath = agentExecutable;
        var isDotnetHost = string.Equals(
            Path.GetFileNameWithoutExtension(processPath),
            "dotnet",
            StringComparison.OrdinalIgnoreCase);

        var leadingArguments = isDotnetHost
            ? $"\"{Assembly.GetEntryAssembly()!.Location}\" --agent"
            : "--agent";

        return new AgentProcessHost(
            processPath,
            timeout: TimeSpan.FromMinutes(2),
            leadingArguments: leadingArguments);
    }

    private static string BuildQuery(ExtractionWindow window)
    {
        var filters = new List<string>();

        // Timestamp is the primary filter. Applying the key with AND would miss
        // late updates to rows whose key is below the checkpoint.
        if (window.From is { } from)
        {
            filters.Add($"updated_at >= '{from.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}'");
        }
        else if (window.AfterKey is { } afterKey)
        {
            filters.Add($"order_id > {afterKey.ToString(CultureInfo.InvariantCulture)}");
        }

        if (window.To is { } to)
        {
            filters.Add($"updated_at <= '{to.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}'");
        }

        var sql =
            """
            select order_id, country, customer_id, product_code, quantity, unit_price, status, updated_at, is_deleted
              from orders
            """;

        if (filters.Count > 0)
        {
            sql += " where " + string.Join(" and ", filters);
        }

        return sql + " order by order_id;";
    }

    private static SourceRow ToSourceRow(IReadOnlyDictionary<string, object?> values)
    {
        var orderId = Convert.ToInt64(values["order_id"], CultureInfo.InvariantCulture);
        var updatedAt = Convert.ToString(values["updated_at"], CultureInfo.InvariantCulture)
                        ?? throw new InvalidOperationException("updated_at is required.");
        var modified = DateTimeOffset.Parse(
            updatedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        return new SourceRow(
            values,
            orderId,
            modified,
            modified.ToString("yyyyMM", CultureInfo.InvariantCulture));
    }

    private static object? ToClr(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };
}
