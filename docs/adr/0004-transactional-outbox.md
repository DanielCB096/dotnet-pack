# ADR 0004: Transactional outbox for batch events

## Context

When a batch finishes, other services may need to know. Writing the data and
publishing to a broker are two different systems. A crash between them can lose
an event or publish an event for data that was rolled back.

## Decision

- The sink writes the batch data, the batch record and an outbox row in the
  **same SQLite transaction**.
- A relay reads pending outbox rows, publishes them and marks them processed.
- Delivery is **at-least-once**.
- The consumer stores processed message ids in SQLite and acks duplicates
  without reprocessing.

The sample publishes to `messages.ndjson` by default and to RabbitMQ when
`RABBITMQ_HOST` is set.

## Consequences

- No dual-write problem between the warehouse and the broker.
- A relay crash after publish and before mark causes a duplicate; the consumer
  inbox makes it harmless.
- The outbox table grows until the relay marks rows processed.
- The pattern is shown in `samples/LegacyBridge`; it is not yet a standalone
  package.
