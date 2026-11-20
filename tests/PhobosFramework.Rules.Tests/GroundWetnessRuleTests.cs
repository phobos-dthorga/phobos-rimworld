using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>Ground wetted by rain and dried by warmth, daylight and wind, one figure for a map (LM2).</summary>
public class GroundWetnessRuleTests
{
    const double Soak = 1, Dry = 10, Frozen = 0.1, Night = 0.4;

    static double Step(double wet, double hours, double rain, double celsius = 20, double daylight = 1, double wind = 1) =>
        GroundWetnessRule.Step(wet, hours, rain, celsius, daylight, wind, Soak, Dry, Frozen, Night);

    [Fact]
    public void AnHourOfRainSoaksDryGround()
    {
        Assert.Equal(0.5, Step(0, 0.5, 1), 6);
        Assert.Equal(1, Step(0, 1, 1), 6);
        Assert.Equal(1, Step(0.9, 1, 1), 6);
    }

    [Fact]
    public void GentleRainWetsMoreSlowly()
    {
        // Gains 0.3 an hour and loses the drying pace for the 0.7 not falling (0.07): still wetter each hour.
        Assert.Equal(0.23, Step(0, 1, 0.3), 6);
    }

    [Fact]
    public void SoakedGroundDriesInTenHoursAtTheReference()
    {
        Assert.Equal(0.5, Step(1, 5, 0), 6);
        Assert.Equal(0, Step(1, 10, 0), 6);
    }

    [Fact]
    public void NightColdAndStillAirDryMoreSlowly()
    {
        double day = 1 - Step(1, 1, 0);
        Assert.True(1 - Step(1, 1, 0, daylight: 0) < day);
        Assert.True(1 - Step(1, 1, 0, celsius: 5) < day);
        Assert.True(1 - Step(1, 1, 0, wind: 0) < day);
        Assert.True(1 - Step(1, 1, 0, celsius: 35, wind: 2) > day);
    }

    [Fact]
    public void FrozenGroundBarelyDries()
    {
        Assert.Equal(0.1, GroundWetnessRule.DryingFactor(-5, 1, 1, Frozen, Night), 6);
        Assert.Equal(0.99, Step(1, 1, 0, celsius: -5), 6);
    }

    [Fact]
    public void TheFactorsHoldTheirLimits()
    {
        Assert.Equal(0.5, GroundWetnessRule.DryingFactor(0.0001, 1, 1, Frozen, Night), 3);
        Assert.Equal(2, GroundWetnessRule.DryingFactor(200, 1, 1, Frozen, Night), 6);
        Assert.Equal(0.7, GroundWetnessRule.DryingFactor(20, 1, -5, Frozen, Night), 6);
        Assert.Equal(1.6, GroundWetnessRule.DryingFactor(20, 1, 10, Frozen, Night), 6);
        Assert.Equal(0.4, GroundWetnessRule.DryingFactor(20, 0, 1, Frozen, Night), 6);
    }

    [Fact]
    public void RainEasingOutDriesOnlyInProportion()
    {
        // Half rain: gains 0.5 an hour, loses half the drying pace (0.05), so it still wets.
        Assert.Equal(0.45, Step(0, 1, 0.5), 6);
    }

    [Fact]
    public void UnderfootIsTheWetterOfRainAndGround()
    {
        Assert.Equal(1, GroundWetnessRule.Underfoot(1, 0, false));
        Assert.Equal(0.6, GroundWetnessRule.Underfoot(0, 0.6, false));
        Assert.Equal(0, GroundWetnessRule.Underfoot(1, 1, true));
    }

    [Fact]
    public void NonsenseStaysInBounds()
    {
        Assert.Equal(0, Step(double.NaN, 1, 0));
        Assert.Equal(0.4, Step(0.4, double.NaN, 1));
        Assert.Equal(1, GroundWetnessRule.Step(0, 1, 1, 20, 1, 1, 0, Dry, Frozen, Night));
        Assert.Equal(0.7, GroundWetnessRule.Step(0.7, 1, 0, 20, 1, 1, Soak, 0, Frozen, Night));
    }
}
