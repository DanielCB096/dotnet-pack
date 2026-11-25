# Contributing

Thanks for taking a look at PipelineKit.

## Requirements

- .NET SDK 10.0.400 or newer
- Docker (for RabbitMQ and the observability stack)

## Build and test

```bash
dotnet restore PipelineKit.slnx --configfile NuGet.Config
dotnet build PipelineKit.slnx -c Release
dotnet test PipelineKit.slnx -c Release
dotnet format PipelineKit.slnx --verify-no-changes
```

## Run the sample

```bash
dotnet run --project samples/LegacyBridge/LegacyBridge.Control -c Release -- --reset
```

## Pull requests

1. Keep the change focused.
2. Add or update tests.
3. Update the relevant docs (`README.md`, `docs/`, package README).
4. Run build, tests and format before opening the PR.
5. Describe the problem, the change and the trade-offs.

## Adding a package

- Put it under `src/PipelineKit.<Name>`.
- Add a local `README.md`; it is packed into the NuGet package.
- Add tests under `tests/PipelineKit.<Name>.Tests`.
- Keep infrastructure dependencies out of the core packages.

## Code style

- File-scoped namespaces.
- Nullable enabled.
- Warnings are errors.
- Public APIs need XML docs.
- Comments explain *why*, not *what*.
