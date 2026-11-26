using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RoadHaltRuleTests
{
    [Theory]
    [InlineData(0f, 0.1f, 1f, 6f, 0.1f)]
    [InlineData(5.95f, 0.1f, 1f, 6f, 6f)]
    [InlineData(2f, 0.1f, 1.5f, 12f, 2.15f)]
    [InlineData(3f, 0.1f, -1f, 12f, 3f)]
    public void WorkStopsAtTheJobsTotal(float soFar, float hours, float pace, float total, float expected) =>
        Assert.Equal(expected, RoadHaltRule.Worked(soFar, hours, pace, total), 3);

    [Fact]
    public void ACarefulTestHoleYieldsALittleLess()
    {
        Assert.True(RoadHaltRule.Portions(insectsOn: false) < RoadHaltRule.Portions(insectsOn: true));
        Assert.True(RoadHaltRule.Portions(insectsOn: false) > 0);
    }

    [Theory]
    [InlineData(4f, 8f, 0f, 300, 1200)]
    [InlineData(4f, 8f, 1f, 300, 2400)]
    [InlineData(4f, 8f, 0.5f, 300, 1800)]
    [InlineData(4f, 8f, 2f, 300, 2400)]
    [InlineData(0f, 0f, 0.5f, 0, 1)]
    public void ATilesOreIsTheLumpTimesTheUnitsACell(float min, float max, float value, int perCell, int expected) =>
        Assert.Equal(expected, RoadHaltRule.Units(min, max, value, perCell));

    [Theory]
    [InlineData(1.5f, 1, 1.5f)]
    [InlineData(1.5f, 3, 3f)]
    [InlineData(1.5f, 4, 3.75f)]
    [InlineData(1f, 2, 1.5f)]
    [InlineData(1.5f, 0, 0f)]
    [InlineData(1.5f, 5, 0f)]
    public void AQuarryGivesMoreInHillsAndWithAHammer(float bit, int hilliness, float expected) =>
        Assert.Equal(expected, RoadHaltRule.QuarryRate(bit, hilliness), 3);

    [Fact]
    public void TheBoreUsesTheSameWorkedHours() =>
        Assert.Equal(RoadHaltRule.Worked(7.95f, 0.1f, 1f, BoreRule.DrillHours), BoreRule.Drilled(7.95f, 0.1f, 1f));
}

public class AmbushRuleTests
{
    [Fact]
    public void FightingIsNoRoll() => Assert.Equal(0f, AmbushRule.Chance(AmbushChoice.Fight, 1f, 1f, true));

    [Fact]
    public void AVehicleThatCannotMoveRulesOutAWayOut()
    {
        Assert.Equal(0f, AmbushRule.Chance(AmbushChoice.DriveThrough, 1f, 1f, false));
        Assert.Equal(0f, AmbushRule.Chance(AmbushChoice.TurnBack, 1f, 1f, false));
    }

    [Fact]
    public void TurningBackIsEasierThanDrivingThrough()
    {
        for (float s = 0f; s <= 1f; s += 0.25f)
        for (float a = 0f; a <= 1f; a += 0.25f)
            Assert.True(AmbushRule.Chance(AmbushChoice.TurnBack, s, a, true) >= AmbushRule.Chance(AmbushChoice.DriveThrough, s, a, true));
    }

    [Fact]
    public void ArmourAndSpeedRaiseTheOddsButNeverToCertain()
    {
        float bare = AmbushRule.Chance(AmbushChoice.DriveThrough, 0f, 0f, true);
        float armoured = AmbushRule.Chance(AmbushChoice.DriveThrough, 0f, 1f, true);
        float fast = AmbushRule.Chance(AmbushChoice.DriveThrough, 1f, 0f, true);
        float both = AmbushRule.Chance(AmbushChoice.DriveThrough, 1f, 1f, true);
        Assert.Equal(AmbushRule.DriveThroughBase, bare, 3);
        Assert.True(armoured > bare && fast > bare && both >= armoured && both >= fast);
        Assert.True(both <= AmbushRule.Most);
        Assert.True(AmbushRule.Chance(AmbushChoice.TurnBack, 1f, 1f, true) <= AmbushRule.Most);
    }

    [Theory]
    [InlineData(0f, 12f)]
    [InlineData(1f, 3f)]
    [InlineData(0.5f, 7.5f)]
    [InlineData(2f, 3f)]
    public void ArmourTurnsAwayMostOfAGauntletHit(float armour, float expected) =>
        Assert.Equal(expected, AmbushRule.HitDamage(armour), 3);

    [Theory]
    [InlineData(3300, 3300, 0f)]
    [InlineData(6600, 3300, 0f)]
    [InlineData(1650, 3300, 0.5f)]
    [InlineData(1100, 3300, 1f)]
    [InlineData(500, 3300, 1f)]
    [InlineData(0, 3300, 0f)]
    public void SpeedCountsFromWalkingPace(int convoy, int walking, float expected) =>
        Assert.Equal(expected, AmbushRule.SpeedTerm(convoy, walking), 3);

    [Theory]
    [InlineData(0f, 2f, 0f)]
    [InlineData(1f, 2f, 0.5f)]
    [InlineData(5f, 2f, 1f)]
    [InlineData(1f, 0f, 0f)]
    public void ArmourCountsToTheReference(float rating, float reference, float expected) =>
        Assert.Equal(expected, AmbushRule.ArmourTerm(rating, reference), 3);
}

public class GroundRuleTests
{
    [Theory]
    [InlineData(0.8f, 500f, 1, 10f, 0.8f)]
    [InlineData(0.2f, 2100f, 1, 10f, 0.5f)]
    [InlineData(0.2f, 3000f, 2, 10f, 1f)]
    [InlineData(0f, 1000f, 1, 10f, 0f)]
    [InlineData(0.9f, 3000f, 1, -5f, 0f)]
    [InlineData(0.9f, 3000f, 3, 10f, 0f)]
    public void SoftGroundIsSwampOrWetFlatlandUnlessFrozen(float swamp, float rain, int hills, float temp, float expected) =>
        Assert.Equal(expected, GroundRule.Softness(swamp, rain, hills, temp), 3);

    [Theory]
    [InlineData(10000f, 1f, false, 0f)]
    [InlineData(20000f, 1f, false, 0.3333f)]
    [InlineData(36000f, 1f, false, 1f)]
    [InlineData(36000f, 0.5f, false, 0.5f)]
    [InlineData(36000f, 1f, true, 0.25f)]
    public void HeavierVehiclesSinkSoonerAndTracksSpreadTheWeight(float kg, float soft, bool tracked, float expected) =>
        Assert.Equal(expected, GroundRule.Sink(kg, soft, tracked), 3);

    [Fact]
    public void TheFastestWayOutIsTaken()
    {
        Assert.Equal(BogRecovery.Winch, GroundRule.Best(winch: true, tow: true));
        Assert.Equal(BogRecovery.Tow, GroundRule.Best(winch: false, tow: true));
        Assert.Equal(BogRecovery.Wait, GroundRule.Best(winch: false, tow: false));
        Assert.True(GroundRule.Hours(BogRecovery.Winch) < GroundRule.Hours(BogRecovery.Tow));
        Assert.True(GroundRule.Hours(BogRecovery.Tow) < GroundRule.Hours(BogRecovery.Wait));
        Assert.Equal(0f, GroundRule.Hours(BogRecovery.Stone));
    }

    [Fact]
    public void GritTakesMostOfTheWinterAway() => Assert.Equal(0.5f, GroundRule.Gritted(2f), 3);
}

public class TrailerFaultRuleTests
{
    [Theory]
    [InlineData(50f, 100f, 1f, 48f)]
    [InlineData(1f, 100f, 1f, 0f)]
    [InlineData(50f, 100f, 0f, 50f)]
    [InlineData(50f, 100f, -2f, 50f)]
    public void DraggedGearWearsFurther(float health, float max, float hours, float expected) =>
        Assert.Equal(expected, TrailerFaultRule.AfterDrag(health, max, hours), 3);

    [Fact]
    public void ACrawlIsSlowButStillMoves() => Assert.InRange(TrailerFaultRule.CrawlShare, 0.05f, 0.5f);
}
