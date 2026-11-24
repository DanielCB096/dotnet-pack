namespace PipelineKit.Quality;

/// <summary>Helpers to compare source and target aggregates.</summary>
public static class Reconciliation
{
    public static ReconciliationResult CompareCounts(long sourceCount, long targetCount)
    {
        var match = sourceCount == targetCount;
        return new ReconciliationResult(
            match,
            sourceCount,
            targetCount,
            null,
            null,
            match ? null : $"Count mismatch: source {sourceCount}, target {targetCount}.");
    }

    public static ReconciliationResult CompareSums(
        decimal sourceSum,
        decimal targetSum,
        decimal tolerance = 0m)
    {
        var difference = Math.Abs(sourceSum - targetSum);
        var match = difference <= tolerance;
        return new ReconciliationResult(
            match,
            null,
            null,
            sourceSum,
            targetSum,
            match ? null : $"Sum mismatch: source {sourceSum}, target {targetSum}, difference {difference}.");
    }
}
