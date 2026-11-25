# PipelineKit.Templates

`dotnet new` template for a PipelineKit incremental job.

## Install

```bash
dotnet new install PipelineKit.Templates::0.7.0
dotnet new pipelinekit-job -n MyPipeline
```

The generated project includes:

- SQLite checkpoints,
- an incremental source and sink,
- a quality rule and quarantine,
- a `PipelineScope` for telemetry.

Replace `DemoSource` and `DemoSink` with your real source and destination.

Docs: https://github.com/DanielCB096/dotnet-pack/blob/main/templates/pipelinekit-job/README.md
