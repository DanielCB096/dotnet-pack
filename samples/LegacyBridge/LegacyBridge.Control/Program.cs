using LegacyBridge.Control;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PipelineKit.Incremental;
using PipelineKit.Telemetry;
using PipelineKit.Telemetry.OpenTelemetry;

if (args.Contains("--agent"))
{
    return await AgentProgram.RunAsync(args);
}

var options = ControlOptions.Parse(args);

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(console =>
    {
        console.SingleLine = true;
        console.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var resource = PipelineTelemetryResource.Create("legacybridge", "0.3.0");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resource)
    .AddPipelineKitTracing()
    .AddPipelineKitConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resource)
    .AddPipelineKitMetrics()
    .AddPipelineKitConsoleExporter()
    .Build();

Console.WriteLine("LegacyBridge — ETL incremental con agente aislado");
Console.WriteLine($"Datos: {options.DataDirectory}");
Console.WriteLine();

var host = new PipelineHost(options, TimeProvider.System, loggerFactory);
await host.InitializeAsync();

Print("Run 1 (carga inicial)", await host.RunAsync());
Print("Run 2 (sin cambios)", await host.RunAsync());

await new LegacyDatabase(options.LegacyDbPath).MutateAsync();
Console.WriteLine("Legado modificado: orden 2 actualizada, orden 9 insertada, orden 7 borrada.");
Print("Run 3 (incremental)", await host.RunAsync());

var reconciled = await host.ReconcileDeletesAsync();
Console.WriteLine($"Reconciliación de borrados: {reconciled} fila(s) marcadas como eliminadas.");

var report = await host.GetReportAsync();
Console.WriteLine();
Console.WriteLine("Resumen del warehouse:");
Console.WriteLine($"  dw_orders:   {report.DwRows} filas ({report.DwDeleted} marcadas como eliminadas)");
Console.WriteLine($"  quarantine:  {report.QuarantineRows} registros");
Console.WriteLine($"  batches:     {report.Batches}");
Console.WriteLine($"  checkpoints: {report.Checkpoints}");

static void Print(string label, IncrementalResult result) =>
    Console.WriteLine(
        $"{label}: extracted={result.RowsExtracted}, written={result.RowsWritten}, " +
        $"unchanged={result.RowsUnchanged}, rejected={result.RowsRejected}, batch={result.BatchId}");

return 0;
