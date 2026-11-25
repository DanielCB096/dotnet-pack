# PipelineKit.AgentProtocol

Versioned JSON contract and process host for isolated extraction agents.

## Install

```bash
dotnet add package PipelineKit.AgentProtocol --version 0.8.0
```

## Why

Some legacy drivers only exist in 32 bits. Loading them in the main process
limits memory and can crash the orchestrator. This package keeps the agent in a
separate process.

## Usage

```csharp
var host = new AgentProcessHost(executablePath, leadingArguments: "--agent");
var result = await host.RunAsync(new AgentRequest(...));
```

The host writes `request.json`, starts the child, reads `progress.ndjson` and
`result.json`, enforces a timeout and kills the process tree if needed.

Targets `netstandard2.0` and `net11.0`, so it works with .NET Framework 4.8 x86
and modern .NET x64.

Docs: https://github.com/DanielCB096/dotnet-pack/blob/main/docs/architecture.md
