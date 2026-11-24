# ADR 0003: Keep infrastructure out of the core packages

## Context

It is tempting to put EF Core, a broker and a cloud SDK in the same package as
the incremental engine. That makes the package easy to demo and hard to reuse.

## Decision

The core packages depend only on `Microsoft.Extensions.*` abstractions and
`System.Diagnostics`. Database, broker and OpenTelemetry adapters are separate
packages.

## Consequences

- A console job, a Worker Service and a test can use the same core.
- Adapters can evolve without breaking the domain.
- More projects, but each one has a single reason to change.
