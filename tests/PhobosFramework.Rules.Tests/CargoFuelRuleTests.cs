using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class CargoFuelRuleTests
{
    [Fact]
    public void ATankTopsUpAtAQuarter()
    {
        Assert.True(CargoFuelRule.ShouldTopUp(25, 100, 0.25));
        Assert.True(CargoFuelRule.ShouldTopUp(0, 100, 0.25));
        Assert.False(CargoFuelRule.ShouldTopUp(26, 100, 0.25));
        Assert.False(CargoFuelRule.ShouldTopUp(0, 0, 0.25));
    }

    [Fact]
    public void ItPoursWhatFillsTheTankOrWhatIsCarried()
    {
        Assert.Equal(75, CargoFuelRule.TopUpAmount(25, 100, 500));
        Assert.Equal(40, CargoFuelRule.TopUpAmount(25, 100, 40));
        Assert.Equal(74, CargoFuelRule.TopUpAmount(25.5, 100, 500));
        Assert.Equal(0, CargoFuelRule.TopUpAmount(100, 100, 500));
        Assert.Equal(0, CargoFuelRule.TopUpAmount(25, 100, 0));
    }

    [Fact]
    public void AWholeTankTakesAQuarterHour()
    {
        Assert.Equal(625, CargoFuelRule.StopTicks(100, 100, 0.25, 2500), 9);
        Assert.Equal(468.75, CargoFuelRule.StopTicks(75, 100, 0.25, 2500), 9);
        Assert.Equal(0, CargoFuelRule.StopTicks(0, 100, 0.25, 2500), 9);
    }

    [Fact]
    public void WithoutCansTheEmptiestVehicleSetsTheReach()
    {
        var users = new List<FuelUser> { new(100, 1, -1), new(30, 0.5, -1) };
        Assert.Equal(60, CargoFuelRule.BudgetTicks(users, new double[0])!.Value, 9);
    }

    [Fact]
    public void SharedCansFeedEveryVehicleOnThem()
    {
        // Two trucks, 50 and 20 in their tanks, burning 1 and 0.5 a tick, sharing 80 in cans: (50 + 20 + 80) / 1.5 = 100 ticks.
        var users = new List<FuelUser> { new(50, 1, 0), new(20, 0.5, 0) };
        Assert.Equal(100, CargoFuelRule.BudgetTicks(users, new double[] { 80 })!.Value, 9);
    }

    [Fact]
    public void AVehicleOffTheCansStillLimitsTheConvoy()
    {
        // The second vehicle has its top-up switched off: it runs dry at 40 ticks, whatever the cans hold.
        var users = new List<FuelUser> { new(50, 1, 0), new(40, 1, -1) };
        Assert.Equal(40, CargoFuelRule.BudgetTicks(users, new double[] { 500 })!.Value, 9);
    }

    [Fact]
    public void EachFuelHasItsOwnPool()
    {
        var users = new List<FuelUser> { new(10, 1, 0), new(10, 1, 1) };
        Assert.Equal(30, CargoFuelRule.BudgetTicks(users, new double[] { 20, 100 })!.Value, 9);
    }

    [Fact]
    public void NothingBurningGivesNoReach() => Assert.Null(CargoFuelRule.BudgetTicks(new List<FuelUser> { new(10, 0, -1) }, new double[0]));
}
