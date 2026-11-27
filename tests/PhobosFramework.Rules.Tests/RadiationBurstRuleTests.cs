using System;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RadiationShelterRuleTests
{
    [Fact]
    public void TheOpenTakesTheWholeDose() => Assert.Equal(1f, RadiationShelterRule.DosePass(ShelterCover.Open));

    [Fact]
    public void AFrameHousePassesNearlyAll() => Assert.InRange(RadiationShelterRule.DosePass(ShelterCover.LightBuilding), 0.8f, 1f);

    [Fact]
    public void StoneWallsPassAFifthOrLess() => Assert.InRange(RadiationShelterRule.DosePass(ShelterCover.StoneBuilding), 0.1f, 0.2f);

    [Fact]
    public void DeepRockPassesAlmostNothing() => Assert.InRange(RadiationShelterRule.DosePass(ShelterCover.ThickRock), 0.002f, 0.004f);

    [Fact]
    public void MoreMassPassesLess() =>
        Assert.True(RadiationShelterRule.DosePass(ShelterCover.ThickRock) < RadiationShelterRule.DosePass(ShelterCover.ThinRock)
            && RadiationShelterRule.DosePass(ShelterCover.ThinRock) < RadiationShelterRule.DosePass(ShelterCover.StoneBuilding)
            && RadiationShelterRule.DosePass(ShelterCover.StoneBuilding) < RadiationShelterRule.DosePass(ShelterCover.LightBuilding));

    [Fact]
    public void RaysThroughRoofsIgnoreBuildingsButNotRock()
    {
        Assert.Equal(1f, RadiationShelterRule.DosePass(ShelterCover.StoneBuilding, buildingsShield: false));
        Assert.Equal(1f, RadiationShelterRule.DosePass(ShelterCover.LightBuilding, buildingsShield: false));
        Assert.Equal(RadiationShelterRule.DosePass(ShelterCover.ThickRock), RadiationShelterRule.DosePass(ShelterCover.ThickRock, buildingsShield: false));
    }

    [Fact]
    public void AnyRoofShadowsTheHeat()
    {
        Assert.False(RadiationShelterRule.Shadowed(ShelterCover.Open));
        Assert.True(RadiationShelterRule.Shadowed(ShelterCover.LightBuilding));
    }

    [Fact]
    public void CoverFollowsWhatWasOverhead()
    {
        Assert.Equal(ShelterCover.Open, RadiationShelterRule.Cover(false, false, false, true));
        Assert.Equal(ShelterCover.ThickRock, RadiationShelterRule.Cover(true, true, true, false));
        Assert.Equal(ShelterCover.ThinRock, RadiationShelterRule.Cover(true, true, false, false));
        Assert.Equal(ShelterCover.StoneBuilding, RadiationShelterRule.Cover(true, false, false, true));
        Assert.Equal(ShelterCover.LightBuilding, RadiationShelterRule.Cover(true, false, false, false));
    }
}

public class RadiationBurstRuleTests
{
    [Fact]
    public void InsideTheEdgeEveryoneTakesTheEdgeDose()
    {
        Assert.Equal(30f, RadiationBurstRule.Grays(0f, 27f, 30f, 100f));
        Assert.Equal(30f, RadiationBurstRule.Grays(27f, 27f, 30f, 100f));
    }

    [Fact]
    public void BeyondTheEdgeItIsTheInverseSquareTimesTheAir()
    {
        // The Mini-Nuke's figures (radiation-bursts.md): about 5.7 Gy at twice its edge, about 7 at 50 cells.
        float twice = RadiationBurstRule.Grays(54f, 27f, 30f, 100f);
        Assert.Equal(30.0 * 0.25 * Math.Exp(-27.0 / 100.0), twice, 3);
        Assert.InRange(RadiationBurstRule.Grays(50f, 27f, 30f, 100f), 6.9f, 7.0f);
    }

    [Fact]
    public void NoAirLengthLeavesTheInverseSquare() => Assert.Equal(2.5f, RadiationBurstRule.Grays(20f, 10f, 10f, 0f), 4);

    [Fact]
    public void TheDoseFallsWithDistance()
    {
        float last = float.MaxValue;
        for (int r = 10; r < 400; r += 10)
        {
            float d = RadiationBurstRule.Grays(r, 15f, 10f, 40f);
            Assert.True(d <= last);
            last = d;
        }
    }

    [Fact]
    public void TheReachIsWhereTheDoseDropsBelowTheLeastGiven()
    {
        float reach = RadiationBurstRule.Reach(27f, 30f, 100f);
        Assert.True(RadiationBurstRule.Grays(reach - 1f, 27f, 30f, 100f) >= RadiationBurstRule.LeastGrays);
        Assert.True(RadiationBurstRule.Grays(reach + 1f, 27f, 30f, 100f) < RadiationBurstRule.LeastGrays);
        Assert.InRange(reach, 320f, 340f);
    }

    [Fact]
    public void NoDoseNoReach()
    {
        Assert.Equal(0f, RadiationBurstRule.Grays(5f, 10f, 0f, 40f));
        Assert.Equal(0f, RadiationBurstRule.Reach(10f, 0.001f, 40f));
    }

    [Fact]
    public void ATinyEdgeCountsAsOneCell() => Assert.Equal(RadiationBurstRule.Grays(10f, 1f, 5f, 40f), RadiationBurstRule.Grays(10f, 0f, 5f, 40f));
}

public class FalloutRuleTests
{
    [Fact]
    public void TheRateHoldsForTheFirstHourThenFallsByTheSevenTenRule()
    {
        Assert.Equal(1.5, FalloutRule.RateAt(0.5, 1.5), 6);
        Assert.Equal(1.5, FalloutRule.RateAt(1, 1.5), 6);
        // Seven times the age, a tenth of the rate (Glasstone and Dolan, 9.16).
        Assert.InRange(FalloutRule.RateAt(49, 1.5) / FalloutRule.RateAt(7, 1.5), 0.09, 0.105);
    }

    [Fact]
    public void TheStrikesFalloutGivesAboutFiveGraysOnDayOneAndSevenInTwoWeeks()
    {
        // The figures the owner chose on 28 Nov 2026, at 1.5 Gy an hour at the first hour.
        Assert.InRange(FalloutRule.DoseBetween(0, 24, 1.5), 4.8, 5.2);
        Assert.InRange(FalloutRule.DoseBetween(0, 24 * 14, 1.5), 6.4, 7.0);
    }

    [Fact]
    public void SlicesAddUpToTheWhole()
    {
        double whole = FalloutRule.DoseBetween(0, 30, 1.5);
        double parts = 0;
        for (double h = 0; h < 30; h += 0.24) parts += FalloutRule.DoseBetween(h, System.Math.Min(h + 0.24, 30), 1.5);
        Assert.Equal(whole, parts, 6);
    }

    [Fact]
    public void ShelterCutsFalloutMoreTheMoreMassThereIs()
    {
        Assert.Equal(1, FalloutRule.Pass(ShelterCover.Open));
        Assert.True(FalloutRule.Pass(ShelterCover.LightBuilding) > FalloutRule.Pass(ShelterCover.StoneBuilding));
        Assert.True(FalloutRule.Pass(ShelterCover.StoneBuilding) > FalloutRule.Pass(ShelterCover.ThinRock));
        Assert.True(FalloutRule.Pass(ShelterCover.ThinRock) > FalloutRule.Pass(ShelterCover.ThickRock));
    }

    [Fact]
    public void NoRateNoDose()
    {
        Assert.Equal(0, FalloutRule.DoseBetween(0, 10, 0));
        Assert.Equal(0, FalloutRule.DoseBetween(5, 5, 1.5));
    }
}
