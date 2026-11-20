using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class SpallRuleTests
{
    static readonly SpallFigures F = SpallFigures.Design;

    // ---- What a hit throws ----

    [Fact]
    public void ABeatenFaceThrowsSpall() =>
        Assert.Equal(SpallKind.Beaten, SpallRule.KindOf(faceBeaten: true, sharp: true, explosive: false, damage: 40, penetration: 36, plateArmour: 30, F));

    [Fact]
    public void RifleFireBouncingOffThickPlateThrowsNothing() =>
        Assert.Equal(SpallKind.None, SpallRule.KindOf(faceBeaten: false, sharp: true, explosive: false, damage: 20, penetration: 7, plateArmour: 30, F));

    [Fact]
    public void ARoundStoppedJustShortOfThePlateShocksIt() =>
        Assert.Equal(SpallKind.Shock, SpallRule.KindOf(faceBeaten: false, sharp: true, explosive: false, damage: 40, penetration: 25, plateArmour: 30, F));

    [Fact]
    public void AHeavyBlastShocksThePlate() =>
        Assert.Equal(SpallKind.Shock, SpallRule.KindOf(faceBeaten: false, sharp: false, explosive: true, damage: 50, penetration: 15, plateArmour: 30, F));

    [Fact]
    public void AHeavyBlastThatBeatsTheFaceStillOnlyShocksIt() =>
        Assert.Equal(SpallKind.Shock, SpallRule.KindOf(faceBeaten: true, sharp: false, explosive: true, damage: 50, penetration: 40, plateArmour: 30, F));

    [Fact]
    public void ASmallBlastThrowsNothing() =>
        Assert.Equal(SpallKind.None, SpallRule.KindOf(faceBeaten: false, sharp: false, explosive: true, damage: 12, penetration: 4, plateArmour: 30, F));

    [Fact]
    public void ABluntBlowThatIsNotABlastThrowsNothing() =>
        Assert.Equal(SpallKind.None, SpallRule.KindOf(faceBeaten: true, sharp: false, explosive: false, damage: 30, penetration: 40, plateArmour: 30, F));

    // ---- How much ----

    [Fact]
    public void ABeatenPlateThrowsHalfOfWhatItTook() =>
        // The record's worked hit: the plate took 45.6, so a pool of 22.8.
        Assert.Equal(22.8, SpallRule.Pool(SpallKind.Beaten, 45.6, F), 6);

    [Fact]
    public void ShockSpallIsAQuarter() => Assert.Equal(10, SpallRule.Pool(SpallKind.Shock, 40, F), 6);

    [Fact]
    public void NothingThrownIsNoPool() => Assert.Equal(0, SpallRule.Pool(SpallKind.None, 40, F));

    [Fact]
    public void AFragmentPenetratesAThirdOfThePlate() => Assert.Equal(10, SpallRule.FragmentPenetration(30, F), 6);

    // ---- The liner ----

    [Fact]
    public void AFullLinerStopsSeventyPerCent() => Assert.Equal(0.7, SpallRule.LinerStops(1, F), 6);

    [Fact]
    public void AHalfWornLinerStopsLess() => Assert.Equal(0.7 / 3, SpallRule.LinerStops(0.5, F), 6);

    [Fact]
    public void ASpentLinerStopsNothing()
    {
        Assert.Equal(0, SpallRule.LinerStops(0.25, F));
        Assert.Equal(0, SpallRule.LinerStops(0, F));
    }

    [Fact]
    public void TheWorkedLinerWearsByEight()
    {
        double stopped = SpallRule.Pool(SpallKind.Beaten, 45.6, F) * SpallRule.LinerStops(1, F);
        Assert.Equal(16, stopped, 0);
        Assert.Equal(8, SpallRule.LinerWear(stopped, F), 0);
    }

    [Fact]
    public void ANarrowLinerSplitsSpallIntoOneFragment()
    {
        Assert.Equal(1, SpallRule.Fragments(SpallKind.Beaten, narrow: true, F));
        Assert.Equal(3, SpallRule.Fragments(SpallKind.Beaten, narrow: false, F));
        Assert.Equal(1, SpallRule.Fragments(SpallKind.Shock, narrow: false, F));
        Assert.Equal(0, SpallRule.Fragments(SpallKind.None, narrow: false, F));
    }

    [Fact]
    public void TheSprayWidensBelowHalf()
    {
        Assert.True(SpallRule.Narrow(0.6, F));
        Assert.False(SpallRule.Narrow(0.5, F));
    }

    [Fact]
    public void TheHealthTabNamesFourStates()
    {
        Assert.Equal(LinerState.NotFitted, SpallRule.StateOf(fitted: false, 1, F));
        Assert.Equal(LinerState.Sound, SpallRule.StateOf(fitted: true, 0.9, F));
        Assert.Equal(LinerState.Worn, SpallRule.StateOf(fitted: true, 0.4, F));
        Assert.Equal(LinerState.Spent, SpallRule.StateOf(fitted: true, 0.2, F));
    }

    [Theory]
    [InlineData(1.0, 0)]
    [InlineData(0.9, 1)]
    [InlineData(0.75, 1)]
    [InlineData(0.7, 2)]
    [InlineData(0.0, 4)]
    public void AReLineTakesAPanelForEachQuarterMissing(double healthShare, int panels) =>
        Assert.Equal(panels, SpallRule.PanelsToReline(healthShare, F));

    // ---- A truck's first look ----

    [Fact]
    public void ANewTruckStartsWithoutItsLiner() => Assert.Equal(0, SpallRule.StartingHealth(null, fitted: false, maxHealth: 60));

    [Fact]
    public void AVehicleThatCameLinedStartsWhole() => Assert.Equal(60, SpallRule.StartingHealth(null, fitted: true, maxHealth: 60));

    [Fact]
    public void ASavedLinerKeepsItsHealth() => Assert.Equal(34, SpallRule.StartingHealth(34, fitted: true, maxHealth: 60));

    [Fact]
    public void ASavedLinerNeverExceedsItsMaximum() => Assert.Equal(60, SpallRule.StartingHealth(90, fitted: true, maxHealth: 60));
}
