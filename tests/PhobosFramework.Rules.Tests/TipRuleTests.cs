using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TipRuleTests
{
    [Fact]
    public void TheCellBehindTheTailIsInTheFan() => Assert.True(TipRule.InFan(0, 0, 0, -1, 8));

    [Fact]
    public void CellsBehindAreIn() => Assert.True(TipRule.InFan(3, -5, 0, -1, 8));

    [Fact]
    public void CellsForwardOfTheTailAreOut() => Assert.False(TipRule.InFan(0, 1, 0, -1, 8));

    [Fact]
    public void CellsPastTheRadiusAreOut() => Assert.False(TipRule.InFan(0, -9, 0, -1, 8));

    [Fact]
    public void ItFacesTheWayTheTruckDoes() => Assert.True(TipRule.InFan(4, 0, 1, 0, 8));

    [Fact]
    public void AStepComesToWholeThings()
    {
        float allowance = 0f;
        Assert.Equal(8, TipRule.WholeThings(ref allowance, 2f, 4.17f));
        Assert.Equal(0.34, allowance, 2);
    }

    [Fact]
    public void WhatIsLeftOverCarries()
    {
        float allowance = 0.9f;
        Assert.Equal(1, TipRule.WholeThings(ref allowance, 0.5f, 0.4f));
    }
}
