using System;
using System.IO;

namespace PipelineKit.AgentProtocol;

/// <summary>Paths passed by the host to the agent process.</summary>
public sealed record AgentPaths(string RequestPath, string ProgressPath, string ResultPath)
{
    /// <summary>Parses --request, --progress and --result from the command line.</summary>
    public static AgentPaths Parse(string[] args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        string? request = null;
        string? progress = null;
        string? result = null;

        for (var index = 0; index < args.Length - 1; index++)
        {
            switch (args[index])
            {
                case "--request":
                    request = args[++index];
                    break;
                case "--progress":
                    progress = args[++index];
                    break;
                case "--result":
                    result = args[++index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(request) ||
            string.IsNullOrWhiteSpace(progress) ||
            string.IsNullOrWhiteSpace(result))
        {
            throw new ArgumentException(
                "Expected --request, --progress and --result paths.",
                nameof(args));
        }

        return new AgentPaths(request!, progress!, result!);
    }
}

/// <summary>Helpers for the agent side of the contract.</summary>
public static class AgentRunner
{
    /// <summary>Reads and deserializes the request file.</summary>
    public static AgentRequest ReadRequest(string path)
    {
        var json = File.ReadAllText(path);
        return AgentProtocolJson.Deserialize<AgentRequest>(json)
               ?? throw new InvalidOperationException("The request file is empty or invalid.");
    }

    /// <summary>Appends one progress line to the NDJSON file.</summary>
    public static void WriteProgress(string path, AgentProgress progress)
    {
        File.AppendAllText(path, AgentProtocolJson.Serialize(progress) + Environment.NewLine);
    }

    /// <summary>Writes the final result file.</summary>
    public static void WriteResult(string path, AgentResult result)
    {
        File.WriteAllText(path, AgentProtocolJson.Serialize(result));
    }
}
