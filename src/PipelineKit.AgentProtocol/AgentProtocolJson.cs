using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipelineKit.AgentProtocol;

/// <summary>Shared JSON settings for request, progress and result files.</summary>
public static class AgentProtocolJson
{
    /// <summary>Canonical serializer options for every file in the protocol.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
