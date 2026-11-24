using PipelineKit.AgentProtocol;
using Xunit;

namespace PipelineKit.AgentProtocol.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public void Request_roundtrips_through_json()
    {
        var request = new AgentRequest(
            AgentId: "agent-1",
            SourceId: "mx",
            Table: "orders",
            Query: "select * from orders",
            OutputPath: "out.ndjson",
            Period: "202601",
            AfterKey: 42,
            ModifiedAfter: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var json = AgentProtocolJson.Serialize(request);
        var back = AgentProtocolJson.Deserialize<AgentRequest>(json);

        Assert.Equal(request, back);
        Assert.Contains("\"agentId\"", json);
    }

    [Fact]
    public void Agent_paths_are_parsed_from_arguments()
    {
        var paths = AgentPaths.Parse(
        [
            "--request", "request.json",
            "--progress", "progress.ndjson",
            "--result", "result.json",
        ]);

        Assert.Equal("request.json", paths.RequestPath);
        Assert.Equal("progress.ndjson", paths.ProgressPath);
        Assert.Equal("result.json", paths.ResultPath);
    }

    [Fact]
    public void Missing_arguments_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => AgentPaths.Parse(["--request", "request.json"]));
    }

    [Fact]
    public void Exit_codes_are_stable()
    {
        Assert.Equal(0, AgentExitCodes.Success);
        Assert.Equal(2, AgentExitCodes.InvalidRequest);
        Assert.Equal(3, AgentExitCodes.SourceError);
        Assert.Equal(4, AgentExitCodes.Timeout);
        Assert.Equal(5, AgentExitCodes.OutputError);
    }
}
