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

public class GrabRuleTests
{
    [Fact]
    public void ALightStackComesWhole() => Assert.Equal(20, GrabRule.CountForLift(20, 1, 500, 2000));

    [Fact]
    public void TheLiftLimitsAHeavyStack() => Assert.Equal(6, GrabRule.CountForLift(10, 75, 500, 2000));

    [Fact]
    public void TheRoomAboardLimitsIt() => Assert.Equal(2, GrabRule.CountForLift(10, 75, 500, 160));

    [Fact]
    public void OneTooHeavyIsLeft() => Assert.Equal(0, GrabRule.CountForLift(1, 600, 500, 2000));

    [Fact]
    public void AFullTruckTakesNothing() => Assert.Equal(0, GrabRule.CountForLift(5, 1, 500, 0));

    [Fact]
    public void AnExactFitIsTaken() => Assert.Equal(5, GrabRule.CountForLift(5, 100, 500, 2000));
}
