using LegacyBridge.Control;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PipelineKit.Incremental;
using PipelineKit.Telemetry;
using PipelineKit.Telemetry.OpenTelemetry;

/*
 * LegacyBridge CLI.
 *
 * Modes:
 *   (default)  runs the demo: initial load, no-change run, mutation,
 *              incremental run, delete reconciliation and outbox relay.
 *   --agent    internal child-process mode used by AgentProcessHost.
 *
 * Options:
 *   --reset    recreates the legacy and warehouse databases.
 *   --data     chooses the data directory (default: <app>/data).
 *
 * The control process never loads a legacy driver. The agent is a separate
 * process that reads the request and writes NDJSON, progress and result files.
 */

if (args.Contains("--agent"))
{
    return await AgentProgram.RunAsync(args);
}

var options = ControlOptions.Parse(args);
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var useOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);
var resource = PipelineTelemetryResource.Create("legacybridge", "0.6.0");

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(console =>
    {
        console.SingleLine = true;
        console.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Information);

    if (useOtlp)
    {
        builder.AddPipelineKitOpenTelemetry(logging => logging.SetResourceBuilder(resource));
    }
});

var tracerBuilder = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resource)
    .AddPipelineKitTracing();
tracerBuilder = useOtlp
    ? tracerBuilder.AddPipelineKitOtlpExporter()
    : tracerBuilder.AddPipelineKitConsoleExporter();
using var tracerProvider = tracerBuilder.Build();

var meterBuilder = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resource)
    .AddPipelineKitMetrics();
meterBuilder = useOtlp
    ? meterBuilder.AddPipelineKitOtlpExporter()
    : meterBuilder.AddPipelineKitConsoleExporter();
using var meterProvider = meterBuilder.Build();

Console.WriteLine("LegacyBridge â€” ETL incremental con agente aislado");
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
Console.WriteLine($"ReconciliaciÃ³n de borrados: {reconciled} fila(s) marcadas como eliminadas.");

var rabbit = RabbitMqOptions.FromEnvironment();
IOutboxPublisher publisher = rabbit is null
    ? new NdjsonOutboxPublisher(Path.Combine(options.DataDirectory, "messages.ndjson"))
    : new RabbitMqOutboxPublisher(rabbit);

var outboxStore = new SqliteOutboxStore(options.WarehouseDbPath);
var relay = new OutboxRelay(outboxStore, publisher, TimeProvider.System);
var destination = rabbit is null
    ? "messages.ndjson"
    : $"{rabbit.Exchange}/{rabbit.RoutingKey}";

try
{
    var published = await relay.RunOnceAsync();
    Console.WriteLine($"Outbox publicado: {published} evento(s) en {destination}.");

    var secondPass = await relay.RunOnceAsync();
    Console.WriteLine($"Outbox segunda pasada: {secondPass} evento(s) (idempotente).");
}
finally
{
    if (publisher is IAsyncDisposable disposable)
    {
        await disposable.DisposeAsync();
    }
}

var report = await host.GetReportAsync();
Console.WriteLine();
Console.WriteLine("Resumen del warehouse:");
Console.WriteLine($"  dw_orders:   {report.DwRows} filas ({report.DwDeleted} marcadas como eliminadas)");
Console.WriteLine($"  quarantine:  {report.QuarantineRows} registros");
Console.WriteLine($"  batches:     {report.Batches}");
Console.WriteLine($"  checkpoints: {report.Checkpoints}");
Console.WriteLine($"  outbox:      {report.OutboxTotal} eventos ({report.OutboxPending} pendientes)");

static void Print(string label, IncrementalResult result) =>
    Console.WriteLine(
        $"{label}: extracted={result.RowsExtracted}, written={result.RowsWritten}, " +
        $"unchanged={result.RowsUnchanged}, rejected={result.RowsRejected}, batch={result.BatchId}");

return 0;

