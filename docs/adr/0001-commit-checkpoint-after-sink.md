# ADR 0001: Commit the checkpoint after the sink succeeds

## Context

A pipeline reads rows from a source, writes them somewhere and records how far it
got. If the checkpoint advances before the write is durable, a failure loses rows.
If it advances after, the same window may be read twice.

## Decision

`IncrementalRunner` commits the checkpoint **only after** the sink returns a
successful result and the source enumerable was fully consumed.

Delivery is therefore **at-least-once**. The sink is responsible for idempotency,
for example with a natural key plus a row hash and an upsert.

## Consequences

- A failed batch does not move the checkpoint.
- A retried batch may re-read rows; the sink must tolerate duplicates.
- A sink that stops early is treated as a bug and the checkpoint is not committed.
- Batch identifiers are kept for traceability, not as a substitute for a row key.
