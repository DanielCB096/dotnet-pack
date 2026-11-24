using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PipelineKit.Incremental;

/// <summary>Stable row hash used to detect real changes and avoid empty updates.</summary>
public static class RowHasher
{
    public static string Compute(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var builder = new StringBuilder();
        foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value is null
                ? "<null>"
                : Convert.ToString(pair.Value, CultureInfo.InvariantCulture));
            builder.Append('\u001f');
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
