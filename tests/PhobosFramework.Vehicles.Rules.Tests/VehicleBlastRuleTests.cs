using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class VehicleBlastRuleTests
{
    static readonly VehicleBlastFigures F = VehicleBlastFigures.Design;

    // ---- Weight and distance ----

    [Fact]
    public void TheReferenceWeightIsWreckedAtTheFireballsEdge() =>
        Assert.Equal(VehicleBlastFate.Wrecked, VehicleBlastRule.Fate(VehicleBlastRule.Severity(1, 20, F), F));

    [Fact]
    public void AVehicleFourTimesHeavierStandsTwiceTheBlast() =>
        Assert.Equal(VehicleBlastRule.Severity(0.5, 20, F), VehicleBlastRule.Severity(1, 80, F), 6);

    [Fact]
    public void ABusInTheBlastZoneIsWrecked() =>
        // The owner's strike on Gukpoi: a bus 41 cells out, where the blast is about 0.78 of the fireball's edge.
        Assert.Equal(VehicleBlastFate.Wrecked, VehicleBlastRule.Fate(VehicleBlastRule.Severity(0.78, 8, F), F));

    [Fact]
    public void AMainBattleTankAtTheSamePlaceIsOnlyBattered() =>
        Assert.Equal(VehicleBlastFate.Battered, VehicleBlastRule.Fate(VehicleBlastRule.Severity(0.78, 60, F), F));

    [Fact]
    public void ATankFarOutIsUntouched() =>
        Assert.Equal(VehicleBlastFate.Untouched, VehicleBlastRule.Fate(VehicleBlastRule.Severity(0.01, 60, F), F));

    [Fact]
    public void NoBlastDoesNothing() => Assert.Equal(0, VehicleBlastRule.Severity(0, 2, F));

    [Fact]
    public void AFeatherweightCountsAsTheLightestWeight() =>
        Assert.Equal(VehicleBlastRule.Severity(0.2, F.lightestTonnes, F), VehicleBlastRule.Severity(0.2, 0.05, F), 6);

    // ---- Parts ----

    [Fact]
    public void OuterPartsLoseMoreThanInnerOnes() =>
        Assert.True(VehicleBlastRule.PartShare(0.5, outer: true, 0.5, F) > VehicleBlastRule.PartShare(0.5, outer: false, 0.5, F));

    [Fact]
    public void AnEvenRollGivesTheShareItself() =>
        Assert.Equal(0.5 * F.outerShare, VehicleBlastRule.PartShare(0.5, outer: true, 0.5, F), 6);

    [Fact]
    public void ThePartShareNeverPassesTheWholePart() =>
        Assert.Equal(1, VehicleBlastRule.PartShare(5, outer: true, 1, F), 6);

    [Fact]
    public void TheSpreadGoesBothWays()
    {
        double low = VehicleBlastRule.PartShare(0.5, true, 0, F), high = VehicleBlastRule.PartShare(0.5, true, 1, F);
        Assert.True(low < 0.5 * F.outerShare && high > 0.5 * F.outerShare);
    }

    // ---- Heat on soft parts ----

    [Fact]
    public void SoftPartsAreSafeBelowWhereRubberBurns() => Assert.Equal(0, VehicleBlastRule.SoftShare(F.softFrom, F));

    [Fact]
    public void SoftPartsBurnAwayAtTheFireballsEdge() => Assert.Equal(F.softShare, VehicleBlastRule.SoftShare(1, F), 6);

    [Fact]
    public void SoftPartsBurnMoreNearer() =>
        Assert.True(VehicleBlastRule.SoftShare(0.8, F) > VehicleBlastRule.SoftShare(0.5, F));

    // ---- The people aboard ----

    [Fact]
    public void NoHarmBelowWhatShows() => Assert.Equal(0, VehicleBlastRule.CrewHarm(F.least / 2, F));

    [Fact]
    public void AWreckHurtsTheCrewByTheFullHarm() => Assert.Equal(F.crewHarm, VehicleBlastRule.CrewHarm(1, F), 6);

    [Fact]
    public void HarmStopsGrowingAtTheMostSeverity() =>
        Assert.Equal(VehicleBlastRule.CrewHarm(F.crewMostSeverity, F), VehicleBlastRule.CrewHarm(10, F), 6);

    [Fact]
    public void HarmIsSplitIntoBlowsNoneTooLarge() => Assert.Equal(3, VehicleBlastRule.Blows(25, F));

    [Fact]
    public void NoHarmNoBlows() => Assert.Equal(0, VehicleBlastRule.Blows(0, F));

    // ---- The hull and the dose ----

    [Fact]
    public void AnOpenVehicleStopsNothing() => Assert.Equal(1, VehicleBlastRule.HullShare(closedCab: false, 60, F), 6);

    [Fact]
    public void ATankPassesAboutAThird() => Assert.Equal(0.35, VehicleBlastRule.HullShare(closedCab: true, 60, F), 6);

    [Fact]
    public void ACarStopsLittle() => Assert.Equal(0.9, VehicleBlastRule.HullShare(closedCab: true, 2, F), 6);

    [Fact]
    public void HeavierClosedVehiclesPassLess() =>
        Assert.True(VehicleBlastRule.HullShare(true, 20, F) < VehicleBlastRule.HullShare(true, 2, F));
}
