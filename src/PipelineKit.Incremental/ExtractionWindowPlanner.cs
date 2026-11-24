namespace PipelineKit.Incremental;

/// <summary>Calculates the next extraction window from a confirmed checkpoint.</summary>
public static class ExtractionWindowPlanner
{
    public static ExtractionWindow Plan(
        IncrementalRequest request,
        Checkpoint? checkpoint,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Strategy switch
        {
            IncrementalStrategy.TimestampAndKey => new ExtractionWindow(
                From: checkpoint?.LastModified is { } last ? last.Subtract(request.Overlap) : null,
                To: now,
                AfterKey: checkpoint?.LastKey,
                Overlap: request.Overlap),

            IncrementalStrategy.Key => new ExtractionWindow(
                AfterKey: checkpoint?.LastKey,
                Overlap: request.Overlap),

            IncrementalStrategy.Period => new ExtractionWindow(
                Period: checkpoint?.Period ?? request.InitialPeriod,
                Overlap: request.Overlap),

            IncrementalStrategy.Snapshot => new ExtractionWindow(Overlap: request.Overlap),

            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Strategy,
                "Unknown incremental strategy."),
        };
    }
}
