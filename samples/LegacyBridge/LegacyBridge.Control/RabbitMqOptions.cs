namespace LegacyBridge.Control;

/// <summary>RabbitMQ connection and topology settings for the sample.</summary>
public sealed record RabbitMqOptions(
    string HostName,
    int Port,
    string UserName,
    string Password,
    string Exchange,
    string Queue,
    string RoutingKey)
{
    /// <summary>Returns null when RABBITMQ_HOST is not set, so the sample can use NDJSON.</summary>
    public static RabbitMqOptions? FromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return new RabbitMqOptions(
            host,
            ParseInt("RABBITMQ_PORT", 5672),
            Env("RABBITMQ_USER", "pipeline"),
            Env("RABBITMQ_PASSWORD", "pipeline"),
            Env("RABBITMQ_EXCHANGE", "pipeline.events"),
            Env("RABBITMQ_QUEUE", "legacybridge.orders"),
            Env("RABBITMQ_ROUTING_KEY", "orders.batch.loaded"));
    }

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int ParseInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
}
