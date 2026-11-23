using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Lopsided thrust on a twin jet (Framework 0.247.0, per-engine realism step 2).</summary>
public class LopsidedThrustTests
{
    [Fact]
    public void MatchedEnginesCostNothing() => Assert.Equal(1, AirCombatRule.LopsidedThrust(0.15, 0.7, 0.7, 2), 9);

    [Fact]
    public void OneEngineOutCostsTheWholeFigure() => Assert.Equal(0.85, AirCombatRule.LopsidedThrust(0.15, 1, 0, 2), 9);

    [Fact]
    public void AWeakerEngineCostsPartOfIt() => Assert.Equal(1 - 0.15 * 0.25, AirCombatRule.LopsidedThrust(0.15, 1, 0.75, 2), 9);

    [Fact]
    public void EitherSideCountsTheSame() =>
        Assert.Equal(AirCombatRule.LopsidedThrust(0.2, 0.6, 1, 2), AirCombatRule.LopsidedThrust(0.2, 1, 0.6, 2), 9);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void OnlyATwinPulls(int engines) => Assert.Equal(1, AirCombatRule.LopsidedThrust(0.15, 1, 0, engines), 9);

    [Fact]
    public void NoFigureNoPull() => Assert.Equal(1, AirCombatRule.LopsidedThrust(0, 1, 0, 2), 9);

    [Fact]
    public void ANonsenseFigureStaysInBounds() => Assert.Equal(0, AirCombatRule.LopsidedThrust(5, 1, 0, 2), 9);

    [Fact]
    public void OneEngineOutBurnsHalfTheFigureMore() => Assert.Equal(1.075, AirCombatRule.LopsidedThrustFuel(0.15, 0, 1, 2), 9);

    [Fact]
    public void MatchedEnginesBurnNoMore() => Assert.Equal(1, AirCombatRule.LopsidedThrustFuel(0.15, 0.6, 0.6, 2), 9);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void OnlyATwinBurnsMore(int engines) => Assert.Equal(1, AirCombatRule.LopsidedThrustFuel(0.15, 1, 0, engines), 9);

    // Framework 0.254.0: four engines by wing, the C-17's places across it (-5, -4, 4, 5).
    static readonly double[] FourArms = { -5, -4, 4, 5 };

    [Fact]
    public void FourMatchedEnginesAreLevel() => Assert.Equal(0, AirCombatRule.Asymmetry(new[] { 1.0, 1.0, 1.0, 1.0 }, FourArms), 9);

    [Fact]
    public void AnOutboardEngineOutPullsMoreThanAnInboardOne() =>
        Assert.True(AirCombatRule.Asymmetry(new[] { 0.0, 1.0, 1.0, 1.0 }, FourArms) > AirCombatRule.Asymmetry(new[] { 1.0, 0.0, 1.0, 1.0 }, FourArms));

    [Fact]
    public void AWholeWingOutIsTheMostLopsided() => Assert.Equal(1, AirCombatRule.Asymmetry(new[] { 0.0, 0.0, 1.0, 1.0 }, FourArms), 9);

    [Fact]
    public void ATwinByArmsMatchesTheTwinRule() =>
        Assert.Equal(AirCombatRule.LopsidedThrust(0.15, 1, 0.6, 2), AirCombatRule.LopsidedThrust(0.15, AirCombatRule.Asymmetry(new[] { 1.0, 0.6 }, AirCombatRule.TwinArms)), 9);

    [Fact]
    public void EnginesOnTheCentrelineDoNotPull() => Assert.Equal(0, AirCombatRule.Asymmetry(new[] { 1.0, 0.0 }, new[] { 0.0, 0.0 }), 9);
}
