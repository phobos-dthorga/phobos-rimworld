using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class AirRuleTests
{
    [Fact]
    public void TheSeaIsTheReference()
    {
        Assert.Equal(1, AirRule.PressureRatio(0), 9);
        Assert.Equal(1, AirRule.StandardDensityRatio(0), 9);
        Assert.Equal(1, AirRule.DensityRatio(0, 15), 9);
        // Water and anything below the sea read as the sea.
        Assert.Equal(1, AirRule.PressureRatio(-500), 9);
    }

    [Fact]
    public void PressureFallsWithHeightAsTheStandardAtmosphereSays()
    {
        // The standard atmosphere's own tables: 84.6 kPa at 1,500 m and 61.6 kPa at 4,000 m against 101.3 kPa at the sea.
        Assert.InRange(AirRule.PressureRatio(1500), 0.834, 0.835);
        Assert.InRange(AirRule.PressureRatio(4000), 0.608, 0.609);
        Assert.True(AirRule.PressureRatio(3000) < AirRule.PressureRatio(2000));
    }

    [Fact]
    public void DensityFallsALittleLessThanPressure()
    {
        // The record's table: 0.95 at 500 m, 0.86 at 1,500 m, 0.78 at 2,500 m, 0.67 at 4,000 m.
        Assert.Equal(0.953, AirRule.StandardDensityRatio(500), 3);
        Assert.Equal(0.864, AirRule.StandardDensityRatio(1500), 3);
        Assert.Equal(0.781, AirRule.StandardDensityRatio(2500), 3);
        Assert.Equal(0.669, AirRule.StandardDensityRatio(4000), 3);
        Assert.True(AirRule.StandardDensityRatio(1500) > AirRule.PressureRatio(1500));
    }

    [Fact]
    public void HotAirIsThinnerAtTheSameHeight()
    {
        Assert.True(AirRule.DensityRatio(1000, 35) < AirRule.DensityRatio(1000, 5));
        Assert.Equal(AirRule.PressureRatio(1000) * 288.15 / 308.15, AirRule.DensityRatio(1000, 35), 9);
    }

    [Fact]
    public void AnEngineWithoutATurbochargerLosesALittleMoreThanTheAirDoes()
    {
        Assert.Equal(1, AirRule.NaturallyAspiratedPower(1), 9);
        // The textbook's correction: 1.132 times the density ratio, less 0.132. Its own example: 160 hp at 0.75 gives 114.7 hp.
        Assert.Equal(1.132 * 0.8637 - 0.132, AirRule.NaturallyAspiratedPower(0.8637), 9);
        Assert.Equal(114.7, 160 * AirRule.NaturallyAspiratedPower(0.75), 1);
        Assert.Equal(0, AirRule.NaturallyAspiratedPower(0.05), 9);
        double at1500 = AirRule.PistonPower(1500, forcedInduction: false, holdsToMetres: 1500);
        Assert.InRange(at1500, 0.84, 0.85);
        Assert.True(at1500 < AirRule.StandardDensityRatio(1500));
    }

    [Fact]
    public void ATurbochargerHoldsPowerToItsLimitThenFalls()
    {
        Assert.Equal(1, AirRule.PistonPower(0, true, 1500), 9);
        Assert.Equal(1, AirRule.PistonPower(1500, true, 1500), 9);
        // Cummins' sheet for one turbocharged engine: full power to about 1,500 m, then 3.4 % for every 305 m. Ours over 1,000 m:
        double at2500 = AirRule.PistonPower(2500, true, 1500);
        Assert.InRange(at2500, 0.88, 0.90);
        // Above its limit it is still far better off than an engine without one.
        Assert.True(at2500 > AirRule.PistonPower(2500, false, 1500));
        // And it falls smoothly from the limit, with no step.
        Assert.InRange(AirRule.PistonPower(1510, true, 1500), 0.998, 1);
    }

    [Fact]
    public void ThinAirCoolsLessButNotQuiteInStep()
    {
        Assert.Equal(1, AirRule.Cooling(0, 0.8), 9);
        double at1500 = AirRule.Cooling(1500, 0.8);
        Assert.InRange(at1500, 0.86, 0.87);
        Assert.True(at1500 > AirRule.PressureRatio(1500));
        Assert.Equal(AirRule.PressureRatio(1500), AirRule.Cooling(1500, 1), 9);
    }
}
