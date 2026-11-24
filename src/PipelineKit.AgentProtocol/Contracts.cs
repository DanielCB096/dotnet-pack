using System;

namespace PipelineKit.AgentProtocol;

/// <summary>Work requested from an extraction agent.</summary>
public sealed record AgentRequest(
    string AgentId,
    string SourceId,
    string Table,
    string Query,
    string OutputPath,
    string? Period = null,
    long? AfterKey = null,
    DateTimeOffset? ModifiedAfter = null);

/// <summary>Progress line emitted by the agent.</summary>
public sealed record AgentProgress(
    string Stage,
    long Rows,
    string? Message = null);

/// <summary>Final result of one agent execution.</summary>
public sealed record AgentResult(
    int ExitCode,
    long Rows,
    long Rejected,
    string? Error = null)
{
    public static AgentResult Ok(long rows, long rejected = 0) => new(0, rows, rejected);

    public static AgentResult Failure(int exitCode, string error) => new(exitCode, 0, 0, error);
}

/// <summary>Stable exit codes for the agent process.</summary>
public static class AgentExitCodes
{
    public const int Success = 0;
    public const int InvalidRequest = 2;
    public const int SourceError = 3;
    public const int Timeout = 4;
    public const int OutputError = 5;
}
