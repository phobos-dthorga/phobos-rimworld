using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class PowerUnitsTests
{
    [Fact]
    public void AmpsAndVoltsMakeWatts()
    {
        Assert.Equal(240, PowerUnits.Watts(10, 24), 9);
        Assert.Equal(10, PowerUnits.Amps(240, 24), 9);
        Assert.Equal(0, PowerUnits.Amps(240, 0), 9);
        Assert.Equal(1.5, PowerUnits.Kilowatts(1500), 9);
    }

    [Fact]
    public void AMachineTakesMoreThanItGives() => Assert.Equal(200, PowerUnits.Input(100, 0.5), 9);

    [Fact]
    public void AnEfficiencyOffTheEndIsHeldInBounds()
    {
        Assert.Equal(100 / 0.05, PowerUnits.Input(100, 0), 9);
        Assert.Equal(100, PowerUnits.Input(100, 1.5), 9);
        Assert.Equal(0, PowerUnits.Input(-5, 0.5), 9);
    }

    [Fact]
    public void TheGamesWattsGoBothWays() => Assert.Equal(300, PowerUnits.ToGameWatts(PowerUnits.FromGameWatts(300)), 9);
}
