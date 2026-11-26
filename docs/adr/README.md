# Architecture decisions

| ADR | Decision |
|---|---|
| [0001](0001-commit-checkpoint-after-sink.md) | Commit the checkpoint only after the sink succeeds |
| [0002](0002-isolate-agent-with-json-contract.md) | Isolate the legacy agent behind a JSON contract |
| [0003](0003-no-infrastructure-in-core.md) | Keep infrastructure out of the core packages |
| [0004](0004-transactional-outbox.md) | Use a transactional outbox for batch events |
| [0005](0005-trusted-publishing.md) | Use NuGet Trusted Publishing for GitHub Actions |
| [0006](0006-observability-stack.md) | Use OpenTelemetry for batch pipelines |
| [0007](0007-net11-baseline.md) | Target .NET 11 for the 0.8 line |
