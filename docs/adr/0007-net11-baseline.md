# ADR 0007: Target .NET 11 for the 0.8 line

## Context

The 11.0 package wave ships with .NET 11. EF Core 11.0.0 and
Microsoft.EntityFrameworkCore.Sqlite 11.0.0 only provide `net11.0` assets, while
Microsoft.Extensions.* and System.Text.Json provide both `net10.0` and
`net11.0`. .NET 11 is an STS release; .NET 10 remains the current LTS.

PipelineKit 0.7.0 targeted `net10.0` and consumed the 10.0 servicing packages.
Keeping that TFM would have meant staying on EF Core 10 and delaying the 11.0
dependencies.

## Decision

- All projects target `net11.0`.
- `PipelineKit.AgentProtocol` keeps `netstandard2.0` alongside `net11.0` so the
  isolated x86 agent still works on .NET Framework.
- The SDK is pinned to `11.0.100` in `global.json`.
- CI uses `dotnet-version: 11.0.x`.

## Consequences

- 0.8.0 is a breaking release for consumers on `net10.0`: packages no longer
  contain `net10.0` assets.
- The project can use EF Core 11 and the 11.0 `Microsoft.Extensions.*` packages
  without conditional versions.
- Consumers that need the LTS runtime must stay on 0.7.0 until multi-targeting is
  evaluated.
- Before 1.0.0, the team must decide whether to add `net10.0` assets. That would
  require EF Core 10 for the `net10.0` target and a dual test matrix; see D3 in
  `docs/roadmap-1.0.md`.
- The baseline is reviewed at every .NET STS/LTS transition.

## Alternatives considered

- **Stay on `net10.0`**: keeps LTS consumers, but blocks EF Core 11.
- **Multi-target `net10.0;net11.0` now**: broadest reach, but EF Core 11 only
  ships `net11.0`, so it needs conditional package versions and doubles the test
  matrix. Deferred to the 1.0 decision.
