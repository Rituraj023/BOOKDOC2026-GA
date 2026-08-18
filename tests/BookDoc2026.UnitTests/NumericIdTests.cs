using BookDoc2026.Domain.Common;

namespace BookDoc2026.UnitTests;

public sealed class NumericIdTests
{
    [Fact]
    public void Next_ReturnsPositiveUniqueIncreasingIdentifiers()
    {
        NumericId.ConfigureNode(7);

        var first = NumericId.Next();
        var second = NumericId.Next();

        Assert.True(first > 0);
        Assert.True(second > first);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1024)]
    public void ConfigureNode_RejectsValuesOutsideTenBitRange(int nodeId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericId.ConfigureNode(nodeId));
    }
}
