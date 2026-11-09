using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class DiveRuleTests
{
    const float Move = 3f, Recharge = 4f;

    [Fact]
    public void AtRestAnHourUnderCostsAnHour() => Assert.Equal(11f, DiveRule.ReserveAfter(12f, 12f, 1f, DivePhase.Submerged, false, Move, Recharge), 3);

    [Fact]
    public void MovingBurnsFaster() => Assert.Equal(9f, DiveRule.ReserveAfter(12f, 12f, 1f, DivePhase.Submerged, true, Move, Recharge), 3);

    [Fact]
    public void ABurnFactorBelowOneNeverMakesMovingCheaperThanRest() => Assert.Equal(11f, DiveRule.ReserveAfter(12f, 12f, 1f, DivePhase.Submerged, true, 0.2f, Recharge), 3);

    [Fact]
    public void TheSurfaceRefills() => Assert.Equal(8f, DiveRule.ReserveAfter(4f, 12f, 1f, DivePhase.Surfaced, false, Move, Recharge), 3);

    [Theory]
    [InlineData(DivePhase.Diving)]
    [InlineData(DivePhase.Surfacing)]
    public void NothingChangesWhileTheTanksFloodOrBlow(DivePhase phase) => Assert.Equal(5f, DiveRule.ReserveAfter(5f, 12f, 1f, phase, true, Move, Recharge), 3);

    [Fact]
    public void TheReserveStopsAtEmpty() => Assert.Equal(0f, DiveRule.ReserveAfter(1f, 12f, 5f, DivePhase.Submerged, true, Move, Recharge), 3);

    [Fact]
    public void TheReserveStopsAtFull() => Assert.Equal(12f, DiveRule.ReserveAfter(11f, 12f, 5f, DivePhase.Surfaced, false, Move, Recharge), 3);

    [Fact]
    public void DamagedPartsCutWhatIsHeld() => Assert.Equal(6f, DiveRule.ReserveAfter(10f, 6f, 0f, DivePhase.Surfacing, false, Move, Recharge), 3);

    [Theory]
    [InlineData(1.2f, 12f, 0.1f, true)]
    [InlineData(1.1f, 12f, 0.1f, false)]
    [InlineData(5f, 0f, 0.1f, false)]
    [InlineData(0f, 12f, 0f, false)]
    public void EnoughToDive(float reserve, float capacity, float share, bool expected) => Assert.Equal(expected, DiveRule.EnoughToDive(reserve, capacity, share));

    [Fact]
    public void HoursLeftFollowThePace()
    {
        Assert.Equal(6f, DiveRule.HoursLeft(6f, false, Move), 3);
        Assert.Equal(2f, DiveRule.HoursLeft(6f, true, Move), 3);
    }

    [Theory]
    [InlineData(DivePhase.Diving, 599, DivePhase.Diving)]
    [InlineData(DivePhase.Diving, 600, DivePhase.Submerged)]
    [InlineData(DivePhase.Surfacing, 600, DivePhase.Surfaced)]
    [InlineData(DivePhase.Submerged, 9999, DivePhase.Submerged)]
    [InlineData(DivePhase.Surfaced, 9999, DivePhase.Surfaced)]
    public void Settled(DivePhase phase, int ticks, DivePhase expected) => Assert.Equal(expected, DiveRule.Settled(phase, ticks, 600));

    [Fact]
    public void OnlyAFullyUnderBoatIsHidden()
    {
        Assert.True(DiveRule.Hidden(DivePhase.Submerged));
        Assert.False(DiveRule.Hidden(DivePhase.Diving));
        Assert.False(DiveRule.Hidden(DivePhase.Surfacing));
        Assert.False(DiveRule.Hidden(DivePhase.Surfaced));
    }

    [Fact]
    public void DamageBelowTheSurface()
    {
        Assert.Equal(0f, DiveRule.DamageBelow(40f, false, 0.5f), 3);
        Assert.Equal(20f, DiveRule.DamageBelow(40f, true, 0.5f), 3);
    }

    [Theory]
    [InlineData(true, true, 300, false, true)]
    [InlineData(true, false, 42, false, false)]
    [InlineData(true, false, 42, true, true)]
    [InlineData(true, false, 30, true, false)]
    [InlineData(false, false, 42, true, false)]
    [InlineData(false, true, 300, true, false)]
    public void DeepEnough(bool isWater, bool impassable, int pathCost, bool chestDeep, bool expected) => Assert.Equal(expected, DiveRule.DeepEnough(isWater, impassable, pathCost, chestDeep));

    [Theory]
    [InlineData(1f, 1f, 0.5f, 1f)]
    [InlineData(1f, 0f, 0.5f, 0.5f)]
    [InlineData(0.5f, 1f, 0.5f, 0.5f)]
    [InlineData(0.5f, 0f, 0.5f, 0.25f)]
    public void Visibility(float weather, float glow, float floor, float expected) => Assert.Equal(expected, DiveRule.Visibility(weather, glow, floor), 3);

    [Fact]
    public void AWatcherOnTopOfTheHullHasTheFullChance() => Assert.Equal(0.02f, DiveRule.SpotChance(0.02f, 0f, 25f, false, 3f, 1f, 1f), 4);

    [Fact]
    public void TheChanceFallsWithDistance() => Assert.Equal(0.01f, DiveRule.SpotChance(0.02f, 12.5f, 25f, false, 3f, 1f, 1f), 4);

    [Theory]
    [InlineData(25f)]
    [InlineData(40f)]
    public void NothingIsSpottedAtOrBeyondRange(float distance) => Assert.Equal(0f, DiveRule.SpotChance(0.02f, distance, 25f, false, 3f, 1f, 1f), 4);

    [Fact]
    public void MovingIsEasierToSpot() => Assert.Equal(0.06f, DiveRule.SpotChance(0.02f, 0f, 25f, true, 3f, 1f, 1f), 4);

    [Fact]
    public void AQuietHullAndPoorVisibilityAreHarderToSpot() => Assert.Equal(0.005f, DiveRule.SpotChance(0.02f, 0f, 25f, false, 3f, 0.5f, 0.5f), 4);

    [Fact]
    public void TheChanceNeverExceedsCertainty() => Assert.Equal(1f, DiveRule.SpotChance(0.9f, 0f, 25f, true, 3f, 1f, 2f), 4);

    [Theory]
    [InlineData(99, 100, true)]
    [InlineData(100, 100, false)]
    [InlineData(101, 100, false)]
    public void Tracked(int now, int until, bool expected) => Assert.Equal(expected, DiveRule.Tracked(now, until));

    [Theory]
    [InlineData(0f, false, true)]
    [InlineData(0f, true, false)]
    [InlineData(1f, false, false)]
    public void EmptyForcesUp(float reserve, bool stayDown, bool expected) => Assert.Equal(expected, DiveRule.EmptyForcesUp(reserve, stayDown));

    [Theory]
    [InlineData(DivePhase.Submerged, 0f, true, true)]
    [InlineData(DivePhase.Submerged, 1f, true, false)]
    [InlineData(DivePhase.Submerged, 0f, false, false)]
    [InlineData(DivePhase.Surfacing, 0f, true, false)]
    public void BreathingStaleAir(DivePhase phase, float reserve, bool stayDown, bool expected) => Assert.Equal(expected, DiveRule.BreathingStaleAir(phase, reserve, stayDown));
}
