# LegacyBridge: end-to-end sample

A complete, runnable sample that uses the PipelineKit packages against a
simulated legacy ERP. No real client data, schemas or credentials are included.

## What it demonstrates

- A legacy SQLite database with dirty data (over-length values, invalid range,
  soft delete, hard delete).
- An **isolated agent process** (`--agent`) that extracts rows and writes NDJSON.
- Incremental extraction with a timestamp watermark, overlap and confirmed
  checkpoints.
- Idempotent upserts with row hashes; unchanged rows are counted, not rewritten.
- Data quality rules and a quarantine table.
- Hard-delete reconciliation by comparing source keys with the warehouse.
- OpenTelemetry traces and metrics through `PipelineKit.Telemetry`.

## Run

```bash
dotnet run --project samples/LegacyBridge/LegacyBridge.Control -c Release -- --reset
```

`--reset` recreates both SQLite databases. Data is stored in the control app's
`data` folder. Use `--data <path>` to choose another location.

## Expected output

```text
Run 1 (carga inicial): extracted=8, written=5, unchanged=0, rejected=3
Run 2 (sin cambios): extracted=1, written=0, unchanged=1, rejected=0
Legado modificado: orden 2 actualizada, orden 9 insertada, orden 7 borrada.
Run 3 (incremental): extracted=3, written=2, unchanged=1, rejected=0
Reconciliación de borrados: 1 fila(s) marcadas como eliminadas.
Outbox publicado: 3 evento(s) en messages.ndjson.
Outbox segunda pasada: 0 evento(s) (idempotente).

Resumen del warehouse:
  dw_orders:   7 filas (2 marcadas como eliminadas)
  quarantine:  3 registros
  batches:     3
  checkpoints: 1
  outbox:      3 eventos (0 pendientes)
```

The second run re-reads the boundary row because of the overlap window; the row
hash detects that it did not change, so it is not rewritten.

## Flow

```
legacy.db
   │  agent process (--agent)
   ▼
NDJSON  ──►  PipelineKit.Incremental  ──►  quality rules
                                              │
                              ┌───────────────┴───────────────┐
                              ▼                               ▼
                         quarantine                     stg_orders
                                                              │
                                                              ▼
                                                         dw_orders
                                                              │
                                                              ▼
                                                        checkpoints
                                                              │
                                                              ▼
                                                          outbox
                                                              │
                                        ┌─────────────────────┴─────────────────────┐
                                        ▼                                           ▼
                                  messages.ndjson                        RabbitMQ topic exchange
                                  (default, no broker)                   pipeline.events
                                                                                 │
                                                                                 ▼
                                                                    LegacyBridge.Consumer
                                                                    processed_messages (SQLite)
```

Each batch writes an `orders.batch.loaded` event in the same SQLite
transaction. `OutboxRelay` publishes pending events and marks them processed,
so a second pass publishes zero. The consumer stores processed message ids and
acks duplicates without reprocessing.

## RabbitMQ

```bash
docker compose up -d rabbitmq

RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge/LegacyBridge.Control -c Release -- --reset

RABBITMQ_HOST=localhost RABBITMQ_USER=pipeline RABBITMQ_PASSWORD=pipeline \
  dotnet run --project samples/LegacyBridge.Consumer -c Release -- --once
```

Without `RABBITMQ_HOST`, the sample keeps working with `messages.ndjson`.

Configuration:

| Variable | Default |
|---|---|
| `RABBITMQ_HOST` | unset (NDJSON mode) |
| `RABBITMQ_PORT` | `5672` |
| `RABBITMQ_USER` | `pipeline` |
| `RABBITMQ_PASSWORD` | `pipeline` |
| `RABBITMQ_EXCHANGE` | `pipeline.events` |
| `RABBITMQ_QUEUE` | `legacybridge.orders` |
| `RABBITMQ_ROUTING_KEY` | `orders.batch.loaded` |

## Agent mode

The control executable is also the agent:

```bash
LegacyBridge.Control --agent --request request.json --progress progress.ndjson --result result.json
```

The control process launches itself as a child process, so the extraction never
shares memory with the orchestration code. Publish the executable as `win-x86`
to simulate the 32-bit driver constraint:

```bash
dotnet publish samples/LegacyBridge/LegacyBridge.Control -c Release -r win-x86 --self-contained false
```

## Files

| File | Purpose |
|---|---|
| `LegacyDatabase.cs` | Legacy schema, seed data and mutation |
| `AgentProgram.cs` | Agent mode: SQLite → NDJSON |
| `AgentIncrementalSource.cs` | PipelineKit source backed by the agent process |
| `SqliteBatchSink.cs` | Quality, staging, dimensional upsert, quarantine and outbox |
| `OutboxRelay.cs` | Outbox store, NDJSON publisher and relay |
| `RabbitMqOptions.cs` | RabbitMQ environment settings |
| `RabbitMqOutboxPublisher.cs` | Durable topic exchange publisher |
| `PipelineHost.cs` | Orchestration, reconciliation and report |
| `Program.cs` | CLI and OpenTelemetry console exporter |

The checkpoint store is provided by `PipelineKit.Incremental.Sqlite`.