using PipelineKit.Quality;
using PipelineKit.Testing;
using Xunit;

namespace PipelineKit.Quality.Tests;

public sealed class QualityTests
{
    [Fact]
    public async Task Max_length_rule_quarantines_the_original_value()
    {
        var store = new InMemoryQuarantineStore();
        var evaluator = new QualityEvaluator(
            [new MaxLengthRule("code", maxLength: 3)],
            store);

        var result = await evaluator.EvaluateAsync(
            DirtyDataGenerator.OverLength("code", declaredLength: 3),
            new QuarantineContext("sales", "mx", "orders", BatchId: "b-1"));

        Assert.False(result.IsValid);
        var record = Assert.Single(store.Items);
        Assert.Equal("code", record.Column);
        Assert.Equal("b-1", record.BatchId);
        Assert.Equal(8, record.RawValue!.Length);
    }

    [Fact]
    public async Task Not_null_rule_rejects_missing_values()
    {
        var evaluator = new QualityEvaluator([new NotNullRule("name")]);

        var result = await evaluator.EvaluateAsync(
            DirtyDataGenerator.MissingValue("name"),
            new QuarantineContext("sales", "mx", "orders"));

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Schema_drift_detects_type_length_and_new_columns()
    {
        var expected = new SchemaSnapshot(
        [
            new SchemaColumn("id", "int", IsNullable: false),
            new SchemaColumn("name", "varchar", 10),
        ]);

        var actual = new SchemaSnapshot(
        [
            new SchemaColumn("id", "bigint", IsNullable: false),
            new SchemaColumn("name", "varchar", 20),
            new SchemaColumn("extra", "varchar", 5),
        ]);

        var drift = SchemaDriftDetector.Detect(expected, actual);

        Assert.Contains(drift.Changes, c => c.Kind == SchemaChangeKind.TypeChanged && c.Column == "id");
        Assert.Contains(drift.Changes, c => c.Kind == SchemaChangeKind.LengthChanged && c.Column == "name");
        Assert.Contains(drift.Changes, c => c.Kind == SchemaChangeKind.Added && c.Column == "extra");
    }

    [Fact]
    public void Reconciliation_reports_count_mismatch()
    {
        var result = Reconciliation.CompareCounts(sourceCount: 10, targetCount: 9);

        Assert.False(result.IsMatch);
        Assert.Contains("10", result.Details);
        Assert.Contains("9", result.Details);
    }
}
