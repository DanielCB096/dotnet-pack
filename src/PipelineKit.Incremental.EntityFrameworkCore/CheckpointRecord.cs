namespace PipelineKit.Incremental.EntityFrameworkCore;

/// <summary>Entity that stores one incremental checkpoint.</summary>
public sealed class CheckpointRecord
{
    /// <summary>Pipeline name.</summary>
    public string Pipeline { get; set; } = string.Empty;

    /// <summary>Source identifier, for example a country or instance.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Table or entity name.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Strategy name (TimestampAndKey, Key, Period, Snapshot).</summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>Last confirmed numeric key.</summary>
    public long? LastKey { get; set; }

    /// <summary>Last confirmed modification timestamp.</summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>Last confirmed period (AAAAMM).</summary>
    public string? Period { get; set; }

    /// <summary>Last confirmed row hash.</summary>
    public string? RowHash { get; set; }
}
