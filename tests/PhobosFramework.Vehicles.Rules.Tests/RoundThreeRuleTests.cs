using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The cross-links round three rules built in Framework 0.113.0: the driver (G1, G2), weather on grip (G5) and cook-off (G13).</summary>
public class RoundThreeRuleTests
{
    [Fact]
    public void AFitDriverIsFullyFit() => Assert.Equal(1, DriverRule.Fitness(1, 1, 1, tired: false), 6);

    [Fact]
    public void CapacitiesAboveOneCountAsOne() => Assert.Equal(1, DriverRule.Fitness(1.2, 1.5, 1.1, tired: false), 6);

    [Fact]
    public void TheWorstCapacityDecides() => Assert.Equal(0.6, DriverRule.Fitness(0.6, 1, 1, tired: false), 6);

    [Fact]
    public void OneEyeCostsLittle() => Assert.Equal(0.85, DriverRule.Fitness(1, 0.5, 1, tired: false), 6);

    [Fact]
    public void ABlindDriverIsAtTheBottom() => Assert.True(DriverRule.Fitness(1, 0, 1, tired: false) < 0.6);

    [Fact]
    public void TirednessTakesATenth() => Assert.Equal(DriverRule.TiredFactor, DriverRule.Fitness(1, 1, 1, tired: true), 6);

    [Fact]
    public void FitnessNeverRisesAsACapacityFalls()
    {
        double last = 2;
        for (double c = 1; c >= -0.0001; c -= 0.05)
        {
            double fitness = DriverRule.Fitness(1, c, 1, tired: false);
            Assert.True(fitness <= last + 1e-9);
            last = fitness;
        }
    }

    [Fact]
    public void ClearWeatherKeepsGrip() => Assert.Equal(1, WeatherGripRule.Factor(0, 0, false, 0.85, 0.75, 0.6), 6);

    [Fact]
    public void RainTakesItsFactor() => Assert.Equal(0.85, WeatherGripRule.Factor(1, 0, false, 0.85, 0.75, 0.6), 6);

    [Fact]
    public void GentleSnowTakesLessThanHardSnow()
    {
        double gentle = WeatherGripRule.Factor(1, 0.8, false, 0.85, 0.75, 0.6);
        double hard = WeatherGripRule.Factor(1, 1.2, false, 0.85, 0.75, 0.6);
        Assert.Equal(0.8, gentle, 6);
        Assert.Equal(0.75, hard, 6);
    }

    [Fact]
    public void IceIsWorstAndNotMultiplied() => Assert.Equal(0.6, WeatherGripRule.Factor(1, 1.2, true, 0.85, 0.75, 0.6), 6);

    [Fact]
    public void NoFireOrNoAmmunitionNeverCooksOff()
    {
        Assert.True(double.IsPositiveInfinity(CookOffRule.MeanHoursBetween(0, 100, 0.5)));
        Assert.True(double.IsPositiveInfinity(CookOffRule.MeanHoursBetween(1, 0, 0.5)));
    }

    [Fact]
    public void ASmallerFireCooksOffLessOften() =>
        Assert.True(CookOffRule.MeanHoursBetween(0.25, 100, 0.5) > CookOffRule.MeanHoursBetween(1, 100, 0.5));

    [Fact]
    public void ABlastUsesItsShareButNeverMoreThanIsAboard()
    {
        Assert.Equal(25, CookOffRule.KilogramsPerBlast(100, 0.25, 5), 6);
        Assert.Equal(5, CookOffRule.KilogramsPerBlast(10, 0.25, 5), 6);
        Assert.Equal(3, CookOffRule.KilogramsPerBlast(3, 0.25, 5), 6);
    }

    [Fact]
    public void MoreAmmunitionMakesABiggerBlastUpToTheCap()
    {
        Assert.True(CookOffRule.Radius(100, 1.5, 0.5, 6) > CookOffRule.Radius(1, 1.5, 0.5, 6));
        Assert.Equal(6, CookOffRule.Radius(1e6, 1.5, 0.5, 6), 6);
        Assert.True(CookOffRule.Damage(100, 8, 4, 60) > CookOffRule.Damage(1, 8, 4, 60));
        Assert.Equal(60, CookOffRule.Damage(1e6, 8, 4, 60));
    }
}
