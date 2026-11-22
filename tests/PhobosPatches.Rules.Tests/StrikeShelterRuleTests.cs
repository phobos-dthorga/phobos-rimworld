using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Rules.Tests;

public class StrikeShelterRuleTests
{
    [Fact]
    public void TheOpenTakesTheWholeDose() => Assert.Equal(1f, StrikeShelterRule.DosePass(StrikeCover.Open));

    [Fact]
    public void AFrameHousePassesNearlyAll() => Assert.InRange(StrikeShelterRule.DosePass(StrikeCover.LightBuilding), 0.8f, 1f);

    [Fact]
    public void StoneWallsPassAFifthOrLess() => Assert.InRange(StrikeShelterRule.DosePass(StrikeCover.StoneBuilding), 0.1f, 0.2f);

    [Fact]
    public void DeepRockPassesAlmostNothing() => Assert.InRange(StrikeShelterRule.DosePass(StrikeCover.ThickRock), 0.002f, 0.004f);

    [Fact]
    public void MoreMassPassesLess() =>
        Assert.True(StrikeShelterRule.DosePass(StrikeCover.ThickRock) < StrikeShelterRule.DosePass(StrikeCover.ThinRock)
            && StrikeShelterRule.DosePass(StrikeCover.ThinRock) < StrikeShelterRule.DosePass(StrikeCover.StoneBuilding)
            && StrikeShelterRule.DosePass(StrikeCover.StoneBuilding) < StrikeShelterRule.DosePass(StrikeCover.LightBuilding));

    [Fact]
    public void AnyRoofShadowsTheHeat()
    {
        Assert.False(StrikeShelterRule.Shadowed(StrikeCover.Open));
        Assert.True(StrikeShelterRule.Shadowed(StrikeCover.LightBuilding));
    }

    [Fact]
    public void CoverFollowsWhatWasOverhead()
    {
        Assert.Equal(StrikeCover.Open, StrikeShelterRule.Cover(false, false, false, true));
        Assert.Equal(StrikeCover.ThickRock, StrikeShelterRule.Cover(true, true, true, false));
        Assert.Equal(StrikeCover.ThinRock, StrikeShelterRule.Cover(true, true, false, false));
        Assert.Equal(StrikeCover.StoneBuilding, StrikeShelterRule.Cover(true, false, false, true));
        Assert.Equal(StrikeCover.LightBuilding, StrikeShelterRule.Cover(true, false, false, false));
    }

    [Fact]
    public void FlashBlindnessIsWorseAndLongerAtNight()
    {
        Assert.True(StrikeShelterRule.FlashChanceFactor(0f) > StrikeShelterRule.FlashChanceFactor(1f));
        Assert.True(StrikeShelterRule.FlashSeverity(0f) > 10 * StrikeShelterRule.FlashSeverity(1f));
    }
}
