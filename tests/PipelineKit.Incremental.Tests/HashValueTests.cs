using PipelineKit.Incremental;
using Xunit;

namespace PipelineKit.Incremental.Tests;

public sealed class HashValueTests
{
    [Fact]
    public void Hash_matches_a_known_value()
    {
        var values = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = "x",
        };

        var hash = RowHasher.Compute(values);
        Assert.Equal("833cce2b6de9fe2a1707a9bbd62888af73139174c85d0b647654e62f114b96a0", hash);
    }

    [Fact]
    public void Hash_changes_when_a_value_changes()
    {
        var first = RowHasher.Compute(new Dictionary<string, object?> { ["a"] = 1 });
        var second = RowHasher.Compute(new Dictionary<string, object?> { ["a"] = 2 });

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_encodes_null_as_a_marker()
    {
        var hash = RowHasher.Compute(new Dictionary<string, object?> { ["a"] = null });

        Assert.Equal("c63cfb5f52e0909bffa51511af93cf3c5772ff8610dc7307dd8e6da733c8cdd3", hash);
    }

    [Fact]
    public void Hash_rejects_null_values()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => RowHasher.Compute(null!));

        Assert.Equal("values", exception.ParamName);
    }
}
