# LegacyBridge.Consumer

A RabbitMQ consumer for the `orders.batch.loaded` events produced by the
LegacyBridge outbox.

## What it does

- Declares a durable topic exchange, a durable queue and the binding.
- Reads JSON envelopes: `{ id, type, payload, occurredAt }`.
- Stores each processed `id` in SQLite before acking.
- Acks duplicates without processing them again.
- Requeues transient failures; rejects malformed messages.

Delivery is at-least-once. The SQLite `processed_messages` table makes the
consumer idempotent.

## Run

Start RabbitMQ:

```bash
docker compose up -d rabbitmq
```

Process one message and exit:

```bash
RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge.Consumer -c Release -- --once
```

Run continuously:

```bash
RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge.Consumer -c Release
```

## Configuration

| Variable | Default |
|---|---|
| `RABBITMQ_HOST` | `localhost` |
| `RABBITMQ_PORT` | `5672` |
| `RABBITMQ_USER` | `pipeline` |
| `RABBITMQ_PASSWORD` | `pipeline` |
| `RABBITMQ_EXCHANGE` | `pipeline.events` |
| `RABBITMQ_QUEUE` | `legacybridge.orders` |
| `RABBITMQ_ROUTING_KEY` | `orders.batch.loaded` |
| `LEGACYBRIDGE_DATA` | `<app>/data` |

The consumer database is `LEGACYBRIDGE_DATA/consumer.db`.

## Expected output

```text
Consuming legacybridge.orders from pipeline.events (orders.batch.loaded) at localhost:5672.
processed 054b9c22d6a0466bb39137fa8458ecd9 (orders.batch.loaded)
```

Publishing the same envelope again produces:

```text
duplicate 054b9c22d6a0466bb39137fa8458ecd9 (orders.batch.loaded)
```

## Why SQLite

It keeps the sample self-contained. In production, use the same table in your
operational database or a dedicated inbox table next to your business data.
