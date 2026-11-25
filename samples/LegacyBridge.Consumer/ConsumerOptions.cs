namespace LegacyBridge.Consumer;

/// <summary>Environment-driven consumer settings.</summary>
public sealed record ConsumerOptions(
    string HostName,
    int Port,
    string UserName,
    string Password,
    string Exchange,
    string Queue,
    string RoutingKey,
    string DataDirectory,
    bool Once)
{
    public static ConsumerOptions Parse(string[] args)
    {
        var dataDirectory = Environment.GetEnvironmentVariable("LEGACYBRIDGE_DATA")
            ?? Path.Combine(AppContext.BaseDirectory, "data");

        return new ConsumerOptions(
            Env("RABBITMQ_HOST", "localhost"),
            ParseInt("RABBITMQ_PORT", 5672),
            Env("RABBITMQ_USER", "pipeline"),
            Env("RABBITMQ_PASSWORD", "pipeline"),
            Env("RABBITMQ_EXCHANGE", "pipeline.events"),
            Env("RABBITMQ_QUEUE", "legacybridge.orders"),
            Env("RABBITMQ_ROUTING_KEY", "orders.batch.loaded"),
            dataDirectory,
            args.Contains("--once"));
    }

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int ParseInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
}
