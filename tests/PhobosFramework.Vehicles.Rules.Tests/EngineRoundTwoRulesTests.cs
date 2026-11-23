using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Engines lit in turn, strip debris and side tanks (Framework 0.250.0, per-engine realism round two, steps 9 to 11).</summary>
public class EngineRoundTwoRulesTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.09, 0)]
    [InlineData(0.1, 1)]
    [InlineData(0.19, 1)]
    [InlineData(0.2, 2)]
    [InlineData(5.0, 2)]
    public void EnginesLightOneAfterAnother(double hours, int lit) => Assert.Equal(lit, StartSequenceRule.Lit(hours, 0.1, 2));

    [Fact]
    public void NoWaitLightsAllAtOnce() => Assert.Equal(2, StartSequenceRule.Lit(0, 0, 2));

    [Fact]
    public void NoDebrisOnPavedGround() => Assert.Equal(0, StripDebrisRule.Chance(0.5, 1, 1, 0, 0.1), 9);

    [Fact]
    public void LowIntakesTakeMoreDebris() =>
        Assert.True(StripDebrisRule.Chance(0.5, 1, 1, 1, 0.1) > StripDebrisRule.Chance(0.5, 0.3, 1, 1, 0.1));

    [Fact]
    public void ABarrierStopsMostOfIt() =>
        Assert.Equal(0.02, StripDebrisRule.EventChance(0.1, 1, 1, 0.8), 9);

    [Fact]
    public void SidesSplitTheTotal()
    {
        (double port, double starboard) = SideTankRule.Sides(100, 20);
        Assert.Equal(60, port, 9);
        Assert.Equal(40, starboard, 9);
    }

    [Fact]
    public void ShutEachEngineBurnsItsOwnSide() => Assert.Equal(-10, SideTankRule.AfterBurn(0, 10, 0, open: false), 9);

    [Fact]
    public void OpenTheFullerSideIsBurnedFirst() => Assert.Equal(5, SideTankRule.AfterBurn(15, 5, 5, open: true), 9);

    [Fact]
    public void ShutALeakDrainsItsSideOnly() => Assert.Equal(-8, SideTankRule.AfterLeak(0, 8, 0, open: false), 9);

    [Fact]
    public void OpenALeakDrainsBoth() => Assert.Equal(0, SideTankRule.AfterLeak(0, 8, 0, open: true), 9);

    [Fact]
    public void RefuellingFillsTheEmptierSideFirst() => Assert.Equal(-2, SideTankRule.AfterRefuel(-10, 8), 9);

    [Fact]
    public void ADrySideIsSeen()
    {
        Assert.True(SideTankRule.SideDry(40, -40, 0, 0.01));
        Assert.False(SideTankRule.SideDry(40, -40, 1, 0.01));
    }
}
