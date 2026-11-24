namespace PipelineKit.Incremental.EntityFrameworkCore;

/// <summary>Entity that stores one incremental checkpoint.</summary>
public sealed class CheckpointRecord
{
    public string Pipeline { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public string Strategy { get; set; } = string.Empty;

    public long? LastKey { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public string? Period { get; set; }

    public string? RowHash { get; set; }
}
