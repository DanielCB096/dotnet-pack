using System.Globalization;
using PipelineKit.Quality;
using Xunit;

namespace PipelineKit.Quality.Tests;

public sealed class RuleContractTests
{
    [Fact]
    public void Not_null_rule_reports_its_name_and_message()
    {
        var rule = new NotNullRule("email");

        Assert.Equal("not_null:email", rule.Name);

        var result = Evaluate(rule, Row(("email", " ")));

        Assert.False(result.IsValid);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("not_null:email", issue.Rule);
        Assert.Equal("email", issue.Column);
        Assert.Equal("Value is required.", issue.Message);
    }

    [Fact]
    public void Not_null_rule_rejects_a_present_null_value()
    {
        var result = Evaluate(new NotNullRule("email"), Row(("email", null)));

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Not_null_rule_accepts_a_value()
    {
        var result = Evaluate(new NotNullRule("email"), Row(("email", "a@b.c")));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Max_length_rule_reports_its_name_and_message()
    {
        var rule = new MaxLengthRule("code", maxLength: 3);

        Assert.Equal("max_length:code:3", rule.Name);

        var result = Evaluate(rule, Row(("code", "ABCDEFGH")));

        Assert.False(result.IsValid);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("max_length:code:3", issue.Rule);
        Assert.Equal("Length 8 exceeds the declared maximum of 3.", issue.Message);
        Assert.Equal("ABCDEFGH", issue.RawValue);
    }

    [Fact]
    public void Max_length_rule_accepts_a_present_null_value()
    {
        var result = Evaluate(new MaxLengthRule("code", maxLength: 3), Row(("code", null)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Max_length_rule_accepts_a_value_at_the_limit()
    {
        var result = Evaluate(new MaxLengthRule("code", maxLength: 3), Row(("code", "ABC")));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Range_rule_reports_its_name_and_message()
    {
        var rule = new RangeRule("amount", 0m, 10m);

        Assert.Equal("range:amount:0:10", rule.Name);

        var result = Evaluate(rule, Row(("amount", "11")));

        Assert.False(result.IsValid);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("range:amount:0:10", issue.Rule);
        Assert.Equal("Value 11 is outside [0, 10].", issue.Message);
        Assert.Equal("11", issue.RawValue);
    }

    [Fact]
    public void Range_rule_accepts_a_present_null_value()
    {
        var result = Evaluate(new RangeRule("amount", 0m, 10m), Row(("amount", null)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10")]
    public void Range_rule_accepts_the_boundaries(string value)
    {
        var result = Evaluate(new RangeRule("amount", 0m, 10m), Row(("amount", value)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Range_rule_reports_a_non_numeric_value()
    {
        var result = Evaluate(new RangeRule("amount", 0m, 10m), Row(("amount", "abc")));

        Assert.False(result.IsValid);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("Value is not numeric.", issue.Message);
        Assert.Equal("abc", issue.RawValue);
    }

    [Fact]
    public void Regex_rule_reports_its_name_and_message()
    {
        var rule = new RegexRule("code", "^[A-Z]{2}$");

        Assert.Equal("regex:code", rule.Name);

        var invalid = Evaluate(rule, Row(("code", "abc")));
        Assert.False(invalid.IsValid);
        var issue = Assert.Single(invalid.Issues);
        Assert.Equal("Value does not match ^[A-Z]{2}$.", issue.Message);
        Assert.Equal("abc", issue.RawValue);

        var valid = Evaluate(rule, Row(("code", "AB")));
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void Regex_rule_accepts_a_present_null_value()
    {
        var result = Evaluate(new RegexRule("code", "^[A-Z]{2}$"), Row(("code", null)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Regex_rule_is_culture_invariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var rule = new RegexRule("code", "(?i)^i$");

            var result = Evaluate(rule, Row(("code", "İ")));

            Assert.False(result.IsValid);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Schema_drift_detects_a_removed_column()
    {
        var expected = new SchemaSnapshot([new SchemaColumn("legacy", "varchar", 10)]);
        var actual = new SchemaSnapshot([]);

        var change = Assert.Single(SchemaDriftDetector.Detect(expected, actual).Changes);

        Assert.Equal(SchemaChangeKind.Removed, change.Kind);
        Assert.Equal("legacy", change.Column);
        Assert.Equal("varchar", change.Expected);
        Assert.Null(change.Actual);
    }

    [Fact]
    public void Schema_drift_detects_a_nullability_change()
    {
        var expected = new SchemaSnapshot([new SchemaColumn("name", "varchar", IsNullable: true)]);
        var actual = new SchemaSnapshot([new SchemaColumn("name", "varchar", IsNullable: false)]);

        var change = Assert.Single(SchemaDriftDetector.Detect(expected, actual).Changes);

        Assert.Equal(SchemaChangeKind.NullabilityChanged, change.Kind);
        Assert.Equal("True", change.Expected);
        Assert.Equal("False", change.Actual);
    }

    [Fact]
    public void Compare_counts_has_no_details_when_counts_match()
    {
        var result = Reconciliation.CompareCounts(sourceCount: 3, targetCount: 3);

        Assert.True(result.IsMatch);
        Assert.Null(result.Details);
    }

    [Fact]
    public void Compare_counts_reports_the_exact_message()
    {
        var result = Reconciliation.CompareCounts(sourceCount: 5, targetCount: 4);

        Assert.False(result.IsMatch);
        Assert.Equal("Count mismatch: source 5, target 4.", result.Details);
    }

    [Fact]
    public void Compare_sums_has_no_details_when_sums_match()
    {
        var result = Reconciliation.CompareSums(10m, 10.5m, tolerance: 0.5m);

        Assert.True(result.IsMatch);
        Assert.Null(result.Details);
    }

    [Fact]
    public void Compare_sums_reports_the_exact_message()
    {
        var result = Reconciliation.CompareSums(10m, 11m);

        Assert.False(result.IsMatch);
        Assert.Equal("Sum mismatch: source 10, target 11, difference 1.", result.Details);
    }

    [Fact]
    public void Detect_throws_when_expected_is_null()
    {
        Assert.Throws<ArgumentNullException>(
            () => SchemaDriftDetector.Detect(null!, new SchemaSnapshot([])));
    }

    [Fact]
    public void Detect_throws_when_actual_is_null()
    {
        Assert.Throws<ArgumentNullException>(
            () => SchemaDriftDetector.Detect(new SchemaSnapshot([]), null!));
    }

    private static QualityResult Evaluate(
        IQualityRule rule,
        IReadOnlyDictionary<string, object?> row) =>
        rule.EvaluateAsync(row).AsTask().GetAwaiter().GetResult();

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(value => value.Key, value => value.Value);
}
