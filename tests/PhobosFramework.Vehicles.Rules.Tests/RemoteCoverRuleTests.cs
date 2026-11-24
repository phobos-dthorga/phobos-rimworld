using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Keeping to radio cover: a block cover map turned into the cost of each cell.</summary>
public class RemoteCoverRuleTests
{
    [Fact]
    public void BlocksRoundUpForAPartBlockAtTheEdge()
    {
        Assert.Equal(3, RemoteCoverRule.Blocks(10, 4));
        Assert.Equal(2, RemoteCoverRule.Blocks(8, 4));
        Assert.Equal(10, RemoteCoverRule.Blocks(10, 0));
    }

    [Fact]
    public void EveryCellOfAnUncoveredBlockCostsAndNoOtherDoes()
    {
        // A 10 x 6 map in blocks of 4: three blocks across, two down. The middle block of the top row is uncovered, and the last block has
        // nothing to stand on.
        byte[] blocks =
        {
            RemoteCoverRule.Covered, RemoteCoverRule.Covered, RemoteCoverRule.Covered,
            RemoteCoverRule.Covered, RemoteCoverRule.Uncovered, RemoteCoverRule.NoSample,
        };
        short[] costs = RemoteCoverRule.Costs(blocks, 4, 10, 6, 8);
        Assert.Equal(8, costs[4 + 4 * 10]);
        Assert.Equal(8, costs[7 + 5 * 10]);
        Assert.Equal(0, costs[3 + 4 * 10]);
        Assert.Equal(0, costs[8 + 5 * 10]);
        Assert.Equal(0, costs[4 + 3 * 10]);
        Assert.Equal(1, RemoteCoverRule.UncoveredCount(blocks));
    }

    [Fact]
    public void ACostOfNoughtLeavesTheMapFree()
    {
        byte[] blocks = { RemoteCoverRule.Uncovered };
        Assert.All(RemoteCoverRule.Costs(blocks, 4, 4, 4, 0), c => Assert.Equal(0, c));
    }

    [Fact]
    public void BlocksThatDoNotCoverTheMapAreRefused()
    {
        Assert.Throws<System.ArgumentException>(() => RemoteCoverRule.Costs(new byte[2], 4, 10, 6, 8));
    }
}
