using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace LegacyBridge.Control;

/// <summary>Publishes outbox messages to a RabbitMQ topic exchange.</summary>
public sealed class RabbitMqOutboxPublisher : IOutboxPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqOutboxPublisher(RabbitMqOptions options) => _options = options;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct = default)
    {
        var channel = await EnsureChannelAsync(ct).ConfigureAwait(false);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId,
            Type = message.Type,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
        };

        using var payload = JsonDocument.Parse(message.Payload);
        var envelope = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = message.MessageId,
            ["type"] = message.Type,
            ["payload"] = payload.RootElement.Clone(),
            ["occurredAt"] = message.OccurredAt,
        };

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: _options.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope)),
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is not null)
        {
            return _channel;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_channel is null)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    AutomaticRecoveryEnabled = true,
                };

                _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
                await _channel.ExchangeDeclareAsync(
                    exchange: _options.Exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: ct).ConfigureAwait(false);

                // Declare the queue and binding here too: events must survive
                // until a consumer starts.
                await _channel.QueueDeclareAsync(
                    queue: _options.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: ct).ConfigureAwait(false);

                await _channel.QueueBindAsync(
                    queue: _options.Queue,
                    exchange: _options.Exchange,
                    routingKey: _options.RoutingKey,
                    arguments: null,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }
}
