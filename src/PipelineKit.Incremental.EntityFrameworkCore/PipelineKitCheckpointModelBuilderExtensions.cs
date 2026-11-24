using Microsoft.EntityFrameworkCore;

namespace PipelineKit.Incremental.EntityFrameworkCore;

/// <summary>Adds the checkpoint entity to an existing model.</summary>
public static class PipelineKitCheckpointModelBuilderExtensions
{
    public const string DefaultTableName = "pipeline_checkpoints";

    /// <summary>Adds the checkpoint entity to the model, using the given table name.</summary>
    public static ModelBuilder AddPipelineKitCheckpoints(
        this ModelBuilder modelBuilder,
        string tableName = DefaultTableName)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        modelBuilder.Entity<CheckpointRecord>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => new { e.Pipeline, e.Source, e.Table, e.Strategy });
            entity.Property(e => e.Pipeline).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Table).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Strategy).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Period).HasMaxLength(64);
            entity.Property(e => e.RowHash).HasMaxLength(128);
        });

        return modelBuilder;
    }
}
