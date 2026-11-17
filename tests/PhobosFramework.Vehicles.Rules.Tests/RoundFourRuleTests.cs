using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The cross-links built in Framework 0.123.0: brake and clutch wear by the move (T4), the shorted wiring's pick (G12) and dust in
/// the air filter (G8).</summary>
public class RoundFourRuleTests
{
    [Fact]
    public void AnEmptyVehicleWearsItsBrakesAsBefore() => Assert.Equal(1, BrakeWearRule.MassFactor(5000, 0, 5000, 3), 6);

    [Fact]
    public void ALadenVehicleWearsItsBrakesByItsMass() => Assert.Equal(1.6, BrakeWearRule.MassFactor(8000, 0, 5000, 3), 6);

    [Fact]
    public void ATrailerCountsAgainstTheTractorsBrakes() => Assert.Equal(2, BrakeWearRule.MassFactor(5000, 5000, 5000, 3), 6);

    [Fact]
    public void TheMassFactorHasACeiling() => Assert.Equal(3, BrakeWearRule.MassFactor(5000, 40000, 5000, 3), 6);

    [Fact]
    public void NoEmptyMassReadsOne() => Assert.Equal(1, BrakeWearRule.MassFactor(5000, 0, 0, 3), 6);

    [Fact]
    public void BrakeWorkGrowsWithTheSquareOfSpeed()
    {
        Assert.Equal(1, BrakeWearRule.SpeedFactor(10, 10, 0.25, 1.5), 6);
        Assert.Equal(0.25, BrakeWearRule.SpeedFactor(5, 10, 0.25, 1.5), 6);
        Assert.Equal(1.44, BrakeWearRule.SpeedFactor(12, 10, 0.25, 1.5), 6);
    }

    [Fact]
    public void SpeedFactorKeepsWithinItsLimits()
    {
        Assert.Equal(0.25, BrakeWearRule.SpeedFactor(1, 10, 0.25, 1.5), 6);
        Assert.Equal(1.5, BrakeWearRule.SpeedFactor(30, 10, 0.25, 1.5), 6);
        Assert.Equal(1, BrakeWearRule.SpeedFactor(0, 10, 0.25, 1.5), 6);
    }

    [Fact]
    public void AClutchHoldsUntilItIsWorn()
    {
        Assert.Equal(1, BrakeWearRule.ClutchHolds(0), 6);
        Assert.Equal(1, BrakeWearRule.ClutchHolds(BrakeWearRule.SlipsFrom), 6);
        Assert.Equal(0.5, BrakeWearRule.ClutchHolds(0.95), 6);
        Assert.Equal(BrakeWearRule.WornThroughHolds, BrakeWearRule.ClutchHolds(1), 6);
    }

    [Fact]
    public void AClutchNeverHoldsMoreAsItWears()
    {
        double last = 2;
        for (double wear = 0; wear <= 1.0001; wear += 0.01)
        {
            double holds = BrakeWearRule.ClutchHolds(wear);
            Assert.True(holds <= last + 1e-9);
            last = holds;
        }
    }

    [Fact]
    public void TheShortedCircuitIsTheSameAtEveryLook() => Assert.Equal(WiringRule.Pick(12345, 6, 2), WiringRule.Pick(12345, 6, 2));

    [Fact]
    public void ShortedCircuitsAreDifferentAndInRange()
    {
        for (int seed = -50; seed < 50; seed++)
        {
            int[] picked = WiringRule.Pick(seed, 4, 3);
            Assert.Equal(3, picked.Length);
            Assert.Equal(3, picked.Distinct().Count());
            Assert.All(picked, i => Assert.InRange(i, 0, 3));
        }
    }

    [Fact]
    public void NoCircuitsNoShort()
    {
        Assert.Empty(WiringRule.Pick(7, 0, 1));
        Assert.Single(WiringRule.Pick(7, 1, 3));
    }

    [Fact]
    public void ShortsSpreadOverTheCircuits()
    {
        var seen = Enumerable.Range(0, 200).Select(seed => WiringRule.Pick(seed, 5, 1)[0]).Distinct().Count();
        Assert.Equal(5, seen);
    }

    [Fact]
    public void WetCountryKeepsTheOrdinaryRate() => Assert.Equal(1, DustRule.Factor(1500, 0, false), 6);

    [Fact]
    public void DesertClogsThreeTimesAsFast() => Assert.Equal(3, DustRule.Factor(0, 0, false), 6);

    [Fact]
    public void DustFallsAsRainRises()
    {
        double last = 10;
        for (double rain = 0; rain <= 1200; rain += 25)
        {
            double factor = DustRule.Factor(rain, 0, false);
            Assert.True(factor <= last + 1e-9);
            last = factor;
        }
    }

    [Fact]
    public void ASandstormMultipliesOutsideOnly()
    {
        Assert.Equal(3 * DustRule.SandstormFactor, DustRule.Factor(0, 1, false), 6);
        Assert.Equal(3, DustRule.Factor(0, 1, true), 6);
    }
}
