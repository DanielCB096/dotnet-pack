namespace PipelineKit.Quality;

/// <summary>Compares an expected schema with the actual source schema.</summary>
public static class SchemaDriftDetector
{
    public static SchemaDrift Detect(SchemaSnapshot expected, SchemaSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var changes = new List<SchemaDriftEntry>();
        var expectedByName = expected.ByName;
        var actualByName = actual.ByName;

        foreach (var column in expectedByName.Values)
        {
            if (!actualByName.TryGetValue(column.Name, out var actualColumn))
            {
                changes.Add(new SchemaDriftEntry(
                    SchemaChangeKind.Removed,
                    column.Name,
                    column.DataType,
                    null));
                continue;
            }

            if (!string.Equals(column.DataType, actualColumn.DataType, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new SchemaDriftEntry(
                    SchemaChangeKind.TypeChanged,
                    column.Name,
                    column.DataType,
                    actualColumn.DataType));
            }

            if (column.MaxLength != actualColumn.MaxLength)
            {
                changes.Add(new SchemaDriftEntry(
                    SchemaChangeKind.LengthChanged,
                    column.Name,
                    column.MaxLength?.ToString(),
                    actualColumn.MaxLength?.ToString()));
            }

            if (column.IsNullable != actualColumn.IsNullable)
            {
                changes.Add(new SchemaDriftEntry(
                    SchemaChangeKind.NullabilityChanged,
                    column.Name,
                    column.IsNullable.ToString(),
                    actualColumn.IsNullable.ToString()));
            }
        }

        foreach (var column in actualByName.Values)
        {
            if (!expectedByName.ContainsKey(column.Name))
            {
                changes.Add(new SchemaDriftEntry(
                    SchemaChangeKind.Added,
                    column.Name,
                    null,
                    column.DataType));
            }
        }

        return new SchemaDrift(changes);
    }
}
