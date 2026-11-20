using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class FuelRateRuleTests
{
    [Fact]
    public void ASteadyBurnSettlesAtItsRate()
    {
        // 10 a day burned in slices every 250 ticks, for two days.
        double figure = 0;
        int last = 0;
        double perSlice = 10.0 / FuelRateRule.TicksPerDay * 250;
        for (int tick = 250; tick <= 120000; tick += 250)
        {
            figure = FuelRateRule.Add(figure, last, tick, perSlice);
            last = tick;
        }
        // Read the moment a slice lands it is about 5 % high (a tenth of the fade's time constant per slice), and just before the next
        // about 5 % low; a display reads at any moment between, so on average it is the rate.
        Assert.InRange(FuelRateRule.PerDay(figure, last, last), 10.0, 10.6);
        Assert.InRange(FuelRateRule.PerDay(figure, last, last + 249), 9.4, 10.0);
    }

    [Fact]
    public void AStoppedBurnFadesAway()
    {
        double figure = FuelRateRule.Add(0, 0, 0, 5);
        Assert.True(FuelRateRule.PerDay(figure, 0, 10000) < FuelRateRule.PerDay(figure, 0, 0) * 0.05);
    }

    [Fact]
    public void ARefundLowersTheRate()
    {
        double figure = FuelRateRule.Add(0, 0, 0, 2);
        figure = FuelRateRule.Add(figure, 0, 0, -0.5);
        Assert.Equal(1.5 / FuelRateRule.FadeTicks * FuelRateRule.TicksPerDay, FuelRateRule.PerDay(figure, 0, 0), 9);
    }

    [Fact]
    public void TimeNeverRunsBackwards() => Assert.Equal(3, FuelRateRule.Faded(3, 500, 100), 9);

    [Fact]
    public void VehicleFrameworkBurnsFullMovingHalfStandingNothingParked()
    {
        Assert.Equal(28, FuelRateRule.FrameworkPerDay(28, true, true), 9);
        Assert.Equal(14, FuelRateRule.FrameworkPerDay(28, true, false), 9);
        Assert.Equal(0, FuelRateRule.FrameworkPerDay(28, false, true), 9);
    }

    [Fact]
    public void AnUpgradeThatMovesVehicleFrameworksRateMovesTheFactor()
    {
        // The Harrier's plasteel armour: Vehicle Framework's 150 a day becomes 172.5.
        Assert.Equal(1.15, FuelRateRule.RateFactor(172.5, 150), 9);
        // A fuel-saving upgrade or setting lowers it; none leaves it at 1.
        Assert.Equal(0.8, FuelRateRule.RateFactor(120, 150), 9);
        Assert.Equal(1, FuelRateRule.RateFactor(150, 150), 9);
    }

    [Fact]
    public void ABrokenFigureNeverStopsOrRunsAwayWithTheBurn()
    {
        Assert.Equal(1, FuelRateRule.RateFactor(172.5, 0));
        Assert.Equal(1, FuelRateRule.RateFactor(0, 150));
        Assert.Equal(1, FuelRateRule.RateFactor(double.NaN, 150));
        Assert.Equal(1, FuelRateRule.RateFactor(172.5, double.PositiveInfinity));
        Assert.Equal(FuelRateRule.MaxRateFactor, FuelRateRule.RateFactor(1e9, 1));
        Assert.Equal(FuelRateRule.MinRateFactor, FuelRateRule.RateFactor(1e-9, 1));
    }
}
