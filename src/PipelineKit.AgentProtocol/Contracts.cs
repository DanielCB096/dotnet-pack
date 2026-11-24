using System;

namespace PipelineKit.AgentProtocol;

/// <summary>Work requested from an extraction agent.</summary>
/// <param name="AgentId">Logical agent identifier.</param>
/// <param name="SourceId">Source identifier; the agent decides how to connect.</param>
/// <param name="Table">Table or entity being read.</param>
/// <param name="Query">Provider-specific query.</param>
/// <param name="OutputPath">NDJSON output file.</param>
/// <param name="Period">Optional period (AAAAMM).</param>
/// <param name="AfterKey">Optional key watermark.</param>
/// <param name="ModifiedAfter">Optional timestamp watermark.</param>
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
/// <param name="Stage">Stage name.</param>
/// <param name="Rows">Rows processed so far.</param>
/// <param name="Message">Optional detail.</param>
public sealed record AgentProgress(
    string Stage,
    long Rows,
    string? Message = null);

/// <summary>Final result of one agent execution.</summary>
/// <param name="ExitCode">Stable exit code.</param>
/// <param name="Rows">Rows extracted.</param>
/// <param name="Rejected">Rows rejected by the agent.</param>
/// <param name="Error">Optional error message.</param>
public sealed record AgentResult(
    int ExitCode,
    long Rows,
    long Rejected,
    string? Error = null)
{
    /// <summary>Creates a successful result.</summary>
    public static AgentResult Ok(long rows, long rejected = 0) => new(0, rows, rejected);

    /// <summary>Creates a failed result.</summary>
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
