using PipelineKit.Incremental;
using PipelineKit.Quality;
using PipelineKit.Telemetry;
using PipelineKit.Testing;

// ---------------------------------------------------------------------------
// End-to-end sample:
//   1. Fake legacy rows (one with an over-length value).
//   2. Incremental run with a checkpoint.
//   3. Second run should read nothing.
//   4. Quality rules send the dirty row to quarantine.
//   5. Schema drift and reconciliation examples.
// ---------------------------------------------------------------------------

var clock = new FixedTimeProvider(new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero));
var rows = DirtyDataGenerator.Rows(
    count: 5,
    start: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    valueFactory: index => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = index,
        ["name"] = $"customer-{index}",
        ["code"] = index == 3 ? new string('X', 12) : $"C{index}",
    });

var store = new InMemoryCheckpointStore();
var runner = new IncrementalRunner(store, clock);
var request = new IncrementalRequest
{
    Pipeline = "sales",
    Source = "mx",
    Table = "customers",
    Strategy = IncrementalStrategy.Key,
};

using (var scope = new PipelineScope("sales", "mx", "customers", country: "MX"))
{
    var sink = new CollectingBatchSink();
    var first = await runner.RunAsync(request, new FakeIncrementalSource(rows), sink);
    scope.Stage("extract");
    scope.RowsExtracted(first.RowsExtracted);
    Console.WriteLine($"First run : extracted={first.RowsExtracted}, written={first.RowsWritten}, batch={first.BatchId}");

    var second = await runner.RunAsync(request, new FakeIncrementalSource(rows), new CollectingBatchSink());
    Console.WriteLine($"Second run: extracted={second.RowsExtracted} (checkpoint works)");
}

var quarantine = new InMemoryQuarantineStore();
var evaluator = new QualityEvaluator([new MaxLengthRule("code", maxLength: 3)], quarantine);
var context = new QuarantineContext("sales", "mx", "customers", BatchId: "sample-batch");

foreach (var row in rows)
{
    var result = await evaluator.EvaluateAsync(row.Values, context);
    if (!result.IsValid)
    {
        Console.WriteLine($"Quarantined row {row.Values["id"]}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
    }
}

Console.WriteLine($"Quarantine records: {quarantine.Items.Count}");

var expected = new SchemaSnapshot(
[
    new SchemaColumn("id", "int", IsNullable: false),
    new SchemaColumn("code", "varchar", 3),
]);

var actual = new SchemaSnapshot(
[
    new SchemaColumn("id", "bigint", IsNullable: false),
    new SchemaColumn("code", "varchar", 9),
    new SchemaColumn("segment", "varchar", 20),
]);

var drift = SchemaDriftDetector.Detect(expected, actual);
Console.WriteLine($"Schema drift: {drift.Changes.Count} change(s)");
foreach (var change in drift.Changes)
{
    Console.WriteLine($"  - {change.Kind}: {change.Column} ({change.Expected} -> {change.Actual})");
}

var reconciliation = Reconciliation.CompareCounts(sourceCount: 5, targetCount: 5);
Console.WriteLine($"Reconciliation: match={reconciliation.IsMatch}");
