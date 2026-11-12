using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class PipeRefuelRuleTests
{
    [Fact]
    public void ThePumpLimitsAFullLine()
    {
        // 0.8 h at 30 an hour from a line holding plenty: 24.
        Assert.Equal(24, PipeRefuelRule.FuelThisLook(50, 200, 0.8, 30, 1000, 1), 9);
    }

    [Fact]
    public void ItStopsAtTheTanksRoom()
    {
        Assert.Equal(5, PipeRefuelRule.FuelThisLook(195, 200, 0.8, 30, 1000, 1), 9);
        Assert.Equal(0, PipeRefuelRule.FuelThisLook(200, 200, 0.8, 30, 1000, 1), 9);
    }

    [Fact]
    public void ItTakesOnlyWhatTheLineHolds()
    {
        Assert.Equal(7, PipeRefuelRule.FuelThisLook(0, 200, 0.8, 30, 7, 1), 9);
        Assert.Equal(0, PipeRefuelRule.FuelThisLook(0, 200, 0.8, 30, 0, 1), 9);
    }

    [Fact]
    public void ANetCountingSeveralUnitsToTheFuelDrawsThatMany()
    {
        // A net of 5 units to one fuel holding 20 units gives 4 fuel, and 4 fuel draws 20 units.
        Assert.Equal(4, PipeRefuelRule.FuelThisLook(0, 200, 0.8, 30, 20, 5), 9);
        Assert.Equal(20, PipeRefuelRule.UnitsFor(4, 5), 9);
    }

    [Fact]
    public void ABadRatioMovesNothing()
    {
        Assert.Equal(0, PipeRefuelRule.FuelThisLook(0, 200, 0.8, 30, 100, 0), 9);
    }

    [Fact]
    public void HoursRunFromTheLastLookAndAreCapped()
    {
        Assert.Equal(0, PipeRefuelRule.Hours(-1, 5000, 2500, 1), 9);
        Assert.Equal(0.8, PipeRefuelRule.Hours(1000, 3000, 2500, 1), 9);
        Assert.Equal(1, PipeRefuelRule.Hours(0, 250000, 2500, 1), 9);
    }
}
