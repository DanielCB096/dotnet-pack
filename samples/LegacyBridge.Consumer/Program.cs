using System.Text.Json;
using LegacyBridge.Consumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/*
 * LegacyBridge consumer.
 *
 * Declares the topic exchange, a durable queue and the binding. Each message is
 * processed once: the message id is stored in SQLite before the ack. Duplicates
 * are detected and acked without reprocessing.
 *
 * Run continuously:
 *   dotnet run --project samples/LegacyBridge.Consumer
 *
 * Process one message and exit:
 *   dotnet run --project samples/LegacyBridge.Consumer -- --once
 *
 * Configuration (environment variables):
 *   RABBITMQ_HOST, RABBITMQ_PORT, RABBITMQ_USER, RABBITMQ_PASSWORD,
 *   RABBITMQ_EXCHANGE, RABBITMQ_QUEUE, RABBITMQ_ROUTING_KEY, LEGACYBRIDGE_DATA
 */

var options = ConsumerOptions.Parse(args);
var store = new ProcessedMessageStore(Path.Combine(options.DataDirectory, "consumer.db"));
await store.EnsureSchemaAsync();

var factory = new ConnectionFactory
{
    HostName = options.HostName,
    Port = options.Port,
    UserName = options.UserName,
    Password = options.Password,
    AutomaticRecoveryEnabled = true,
};

await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: options.Exchange,
    type: ExchangeType.Topic,
    durable: true,
    autoDelete: false,
    arguments: null);

await channel.QueueDeclareAsync(
    queue: options.Queue,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

await channel.QueueBindAsync(
    queue: options.Queue,
    exchange: options.Exchange,
    routingKey: options.RoutingKey,
    arguments: null);

await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
var gate = new SemaphoreSlim(1, 1);
var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (_, args) =>
{
    await gate.WaitAsync();
    try
    {
        if (options.Once && done.Task.IsCompleted)
        {
            // Another message arrived before cancellation; leave it for next run.
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
            return;
        }

        var body = args.Body.ToArray();
        using var document = JsonDocument.Parse(body);
        var messageId = document.RootElement.TryGetProperty("id", out var idElement)
            ? idElement.GetString()
            : null;
        var type = document.RootElement.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString()
            : "unknown";

        if (string.IsNullOrWhiteSpace(messageId))
        {
            Console.Error.WriteLine("Message without id; rejecting.");
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (await store.IsProcessedAsync(messageId))
        {
            Console.WriteLine($"duplicate {messageId} ({type})");
        }
        else
        {
            await store.MarkProcessedAsync(messageId, DateTimeOffset.UtcNow);
            Console.WriteLine($"processed {messageId} ({type})");
        }

        await channel.BasicAckAsync(args.DeliveryTag, multiple: false);

        if (options.Once)
        {
            done.TrySetResult(true);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
    }
    finally
    {
        gate.Release();
    }
};

var tag = await channel.BasicConsumeAsync(
    queue: options.Queue,
    autoAck: false,
    consumer: consumer);

Console.WriteLine(
    $"Consuming {options.Queue} from {options.Exchange} ({options.RoutingKey}) at {options.HostName}:{options.Port}.");

if (options.Once)
{
    var completed = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    if (completed != done.Task)
    {
        Console.Error.WriteLine("No message received within 20 seconds.");
        return 1;
    }
}
else
{
    await Task.Delay(Timeout.Infinite);
}

await channel.BasicCancelAsync(tag);
return 0;
