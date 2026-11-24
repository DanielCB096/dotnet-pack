using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PipelineKit.AgentProtocol;

namespace LegacyBridge.Control;

/// <summary>Agent mode. It reads SQLite and writes NDJSON, progress and result files.</summary>
public static class AgentProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        AgentPaths paths;
        try
        {
            paths = AgentPaths.Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return AgentExitCodes.InvalidRequest;
        }

        try
        {
            var request = AgentRunner.ReadRequest(paths.RequestPath);
            AgentRunner.WriteProgress(paths.ProgressPath, new AgentProgress("start", 0));

            await using var connection = new SqliteConnection($"Data Source={request.SourceId}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = request.Query;

            await using var reader = await command.ExecuteReaderAsync();

            Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
            await using var writer = new StreamWriter(
                request.OutputPath,
                append: false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var rows = 0;
            while (await reader.ReadAsync())
            {
                var values = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    values[reader.GetName(index)] = reader.IsDBNull(index)
                        ? null
                        : reader.GetValue(index);
                }

                await writer.WriteLineAsync(JsonSerializer.Serialize(values));
                rows++;

                if (rows % 100 == 0)
                {
                    AgentRunner.WriteProgress(paths.ProgressPath, new AgentProgress("extract", rows));
                }
            }

            await writer.FlushAsync();
            AgentRunner.WriteProgress(paths.ProgressPath, new AgentProgress("done", rows));
            AgentRunner.WriteResult(paths.ResultPath, AgentResult.Ok(rows));
            return AgentExitCodes.Success;
        }
        catch (Exception ex)
        {
            try
            {
                AgentRunner.WriteResult(
                    paths.ResultPath,
                    AgentResult.Failure(AgentExitCodes.SourceError, ex.Message));
            }
            catch
            {
                // Best effort: the host will inspect the exit code.
            }

            Console.Error.WriteLine(ex);
            return AgentExitCodes.SourceError;
        }
    }
}
