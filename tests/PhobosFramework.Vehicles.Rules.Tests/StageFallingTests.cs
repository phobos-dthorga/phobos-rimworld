using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// A warning stage warns only on the way down (Framework 0.383.1; the owner's card of 28 Nov 2026). The worked cases are the battery's two
/// warnings (Systems_Turbine.xml's turbineStart: too weak below 0.25, barely enough to 0.5, nothing above; Systems_Electrical.xml's
/// startingPower: too weak below 0.1, barely enough to 0.2, enough from 0.2).
/// </summary>
public class StageFallingTests
{
    const double Open = double.NaN;
    static readonly IReadOnlyList<double> TurbineFrom = new[] { Open, 0.25 }, TurbineTo = new[] { 0.25, 0.5 };
    static readonly IReadOnlyList<double> PistonFrom = new[] { Open, 0.1, 0.2 }, PistonTo = new[] { 0.1, 0.2, Open };

    [Fact]
    public void RunningDownFromAStageAboveIsFalling() => Assert.True(StageRule.EnteredFalling(2, 1, PistonFrom, PistonTo));

    [Fact]
    public void AFlatBatteryWarmedBackUpIsNotFalling()
    {
        // The Abrams in the owner's save, 28 Nov 2026: too weak at night, barely enough by day, twice a day.
        Assert.False(StageRule.EnteredFalling(0, 1, TurbineFrom, TurbineTo));
        Assert.False(StageRule.EnteredFalling(0, 1, PistonFrom, PistonTo));
    }

    [Fact]
    public void FromAboveEveryStageIsFalling() =>
        // The turbine's stages end at 0.5: a battery above that is in no stage, and coming down into the warning is falling.
        Assert.True(StageRule.EnteredFalling(-1, 1, TurbineFrom, TurbineTo));

    [Fact]
    public void AfterALoadWithBothSidesStagedTheWarningStillComes() =>
        // A load works the stages out again from none; with stages on both sides it cannot tell, and warns, as it always did.
        Assert.True(StageRule.EnteredFalling(-1, 1, PistonFrom, PistonTo));

    [Fact]
    public void FromAGapBelowIsRising()
    {
        IReadOnlyList<double> from = new[] { 0.5 }, to = new[] { Open };
        Assert.False(StageRule.EnteredFalling(-1, 0, from, to));
    }

    [Fact]
    public void EnteringNoStageIsNotFalling() => Assert.False(StageRule.EnteredFalling(1, -1, TurbineFrom, TurbineTo));
}
