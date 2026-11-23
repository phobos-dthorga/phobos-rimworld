using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class StartReserveRuleTests
{
    // The starting power's stages as the electrical system's Def writes them: below 0.1 refuses a start, 0.1 to 0.2 is "barely enough to
    // start", from 0.2 "enough to start".
    static readonly StartReserveRule.Stage[] Electrical =
    {
        new(double.NaN, 0.1f, true),
        new(0.1f, 0.2f, false),
        new(0.2f, double.NaN, false),
    };

    [Fact]
    public void TheLineIsTheTopOfTheBarelyEnoughBandAboveTheNoStartStage() =>
        Assert.Equal(0.2f, StartReserveRule.Line(Electrical), 6);

    [Fact]
    public void WithNoBandAboveItTheLineIsTheNoStartLineItself() =>
        Assert.Equal(0.1f, StartReserveRule.Line(new StartReserveRule.Stage[] { new(double.NaN, 0.1f, true), new(0.1f, double.NaN, false) }), 6);

    [Fact]
    public void AReadingThatRefusesNoStartHasNoLine() =>
        Assert.True(double.IsNaN(StartReserveRule.Line(new StartReserveRule.Stage[] { new(double.NaN, 0.3f, false), new(0.3f, double.NaN, false) })));

    [Fact]
    public void AHouseBatteryOrNoLineNeverHoldsTheLoad()
    {
        Assert.False(StartReserveRule.Held(true, 0.05, 0.2, false, true));
        Assert.False(StartReserveRule.Held(false, 0.05, double.NaN, false, true));
    }

    [Fact]
    public void BelowTheLineTheLoadIsHeldEvenWhileCharging()
    {
        Assert.True(StartReserveRule.Held(false, 0.19, 0.2, false, false));
        Assert.True(StartReserveRule.Held(false, 0.19, 0.2, true, true));
    }

    [Fact]
    public void AboveTheLineAnUnheldLoadRuns() => Assert.False(StartReserveRule.Held(false, 0.5, 0.2, false, false));

    [Fact]
    public void OnceHeldOnlyChargingPastTheLineBringsItBack()
    {
        // A warmer morning lifts the starting power with no charge put in: still held.
        Assert.True(StartReserveRule.Held(false, 0.25, 0.2, false, true));
        // The engine charging it past the line: back on.
        Assert.False(StartReserveRule.Held(false, 0.2, 0.2, true, true));
    }
}
