using Phobos.MissilePatches.Strike;
using Xunit;

namespace Phobos.MissilePatches.Rules.Tests;

public class StrikeRuleTests
{
    [Fact]
    public void ZonesFollowTheRadii()
    {
        Assert.Equal(StrikeZone.Fireball, StrikeRule.Zone(0f, 25f, 110f));
        Assert.Equal(StrikeZone.Fireball, StrikeRule.Zone(25f, 25f, 110f));
        Assert.Equal(StrikeZone.Blast, StrikeRule.Zone(25.1f, 25f, 110f));
        Assert.Equal(StrikeZone.Thermal, StrikeRule.Zone(110.1f, 25f, 110f));
    }

    [Fact]
    public void TheFalloffIsRimatomicsCurve()
    {
        // 250 x -ln(d / 250): about 576 at 25 cells, 205 at 110, nothing at 250 and beyond.
        Assert.InRange(StrikeRule.Falloff(25f, 250f), 575f, 577f);
        Assert.InRange(StrikeRule.Falloff(110f, 250f), 204f, 206f);
        Assert.Equal(0f, StrikeRule.Falloff(250f, 250f));
        Assert.Equal(0f, StrikeRule.Falloff(400f, 250f));
        Assert.Equal(StrikeRule.Falloff(1f, 250f), StrikeRule.Falloff(0f, 250f));
    }

    [Fact]
    public void StrengthIsOneAtTheFireballAndFallsToNothing()
    {
        Assert.Equal(1f, StrikeRule.Strength(10f, 25f, 250f));
        Assert.Equal(1f, StrikeRule.Strength(25f, 25f, 250f), 3);
        Assert.InRange(StrikeRule.Strength(110f, 25f, 250f), 0.3f, 0.4f);
        Assert.Equal(0f, StrikeRule.Strength(250f, 25f, 250f));
    }

    [Fact]
    public void FiresAreFullInTheBlastAndFallToTheEdge()
    {
        Assert.Equal(0.08f, StrikeRule.FireChance(0.08f, 50f, 110f, 250f, 0.15f));
        Assert.Equal(0.08f * 0.15f, StrikeRule.FireChance(0.08f, 250f, 110f, 250f, 0.15f), 5);
        Assert.Equal(0.08f * 0.15f, StrikeRule.FireChance(0.08f, 400f, 110f, 250f, 0.15f), 5);
        float halfway = StrikeRule.FireChance(0.08f, 180f, 110f, 250f, 0.15f);
        Assert.True(halfway < 0.08f && halfway > 0.08f * 0.15f);
        Assert.Equal(0f, StrikeRule.FireChance(0f, 50f, 110f, 250f, 0.15f));
    }

    [Fact]
    public void BurnsSpreadOverSeveralHitsAndAreCapped()
    {
        Assert.Equal(0, StrikeRule.BurnHits(0f, 12f, 10));
        Assert.Equal(1, StrikeRule.BurnHits(10f, 12f, 10));
        Assert.Equal(3, StrikeRule.BurnHits(30f, 12f, 10));
        Assert.Equal(10, StrikeRule.BurnHits(500f, 12f, 10));
    }
}
