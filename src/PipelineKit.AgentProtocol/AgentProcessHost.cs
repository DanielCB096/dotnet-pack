using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PipelineKit.AgentProtocol;

/// <summary>
/// Runs an isolated agent process, enforces a timeout and reads its result file.
/// The host writes request.json, starts the child, reads progress and result,
/// and never loads the legacy driver itself.
/// </summary>
public sealed class AgentProcessHost
{
    private readonly string _executablePath;
    private readonly string? _workingDirectory;
    private readonly TimeSpan _timeout;
    private readonly bool _keepTemporaryFiles;
    private readonly string? _leadingArguments;

    public AgentProcessHost(
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        bool keepTemporaryFiles = false,
        string? leadingArguments = null)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path is required.", nameof(executablePath));
        }

        _executablePath = executablePath;
        _workingDirectory = workingDirectory;
        _timeout = timeout ?? TimeSpan.FromMinutes(10);
        _keepTemporaryFiles = keepTemporaryFiles;
        _leadingArguments = leadingArguments;
    }

    /// <summary>
    /// Starts the agent, waits for completion or timeout, kills the process tree
    /// on timeout and returns the result file or a stable failure code.
    /// </summary>
    public async Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "pipelinekit-agent",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);

        var requestPath = Path.Combine(tempDirectory, "request.json");
        var progressPath = Path.Combine(tempDirectory, "progress.ndjson");
        var resultPath = Path.Combine(tempDirectory, "result.json");

        try
        {
            File.WriteAllText(requestPath, AgentProtocolJson.Serialize(request));

            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = string.Concat(
                    string.IsNullOrWhiteSpace(_leadingArguments)
                        ? string.Empty
                        : _leadingArguments + " ",
                    "--request ", Quote(requestPath),
                    " --progress ", Quote(progressPath),
                    " --result ", Quote(resultPath)),
                WorkingDirectory = _workingDirectory ?? Path.GetDirectoryName(_executablePath) ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return AgentResult.Failure(AgentExitCodes.SourceError, "The agent process could not start.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var exitTask = Task.Run(() => process.WaitForExit());
            var timeoutTask = Task.Delay(_timeout, ct);

            var completed = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);

            if (completed == timeoutTask)
            {
                Kill(process);
                await exitTask.ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ct);
                }

                return AgentResult.Failure(
                    AgentExitCodes.Timeout,
                    $"The agent exceeded the timeout of {_timeout}.");
            }

            await exitTask.ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (File.Exists(resultPath))
            {
                var resultJson = File.ReadAllText(resultPath);
                var result = AgentProtocolJson.Deserialize<AgentResult>(resultJson);
                if (result is not null)
                {
                    return result;
                }
            }

            var error = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            return AgentResult.Failure(
                process.ExitCode == 0 ? AgentExitCodes.OutputError : process.ExitCode,
                string.IsNullOrWhiteSpace(error) ? "The agent did not produce a result file." : error.Trim());
        }
        finally
        {
            if (!_keepTemporaryFiles)
            {
                TryDelete(tempDirectory);
            }
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static void Kill(Process process)
    {
#if NET10_0_OR_GREATER
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
#else
        try
        {
            process.Kill();
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
#endif
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
