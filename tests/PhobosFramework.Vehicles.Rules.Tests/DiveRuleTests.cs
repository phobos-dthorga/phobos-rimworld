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
}
