# ADR 0002: Isolate the legacy agent behind a JSON contract

## Context

Some legacy drivers only exist in 32 bits. Loading them in the main process
limits memory and can crash the orchestrator. A separate process is safer, but
it introduces a contract between two architectures.

## Decision

- The orchestrator writes a `request.json`.
- The agent writes `progress.ndjson` and `result.json`.
- The process exits with a stable code.
- The host enforces a timeout and kills the process tree.
- The contract lives in `PipelineKit.AgentProtocol` and targets
  `netstandard2.0` and `net11.0`.

## Consequences

- The x64 process never loads the 32-bit driver.
- The contract is debuggable with plain files.
- Credentials never travel on the command line.
- Named pipes or gRPC can be added later without changing the request model.
