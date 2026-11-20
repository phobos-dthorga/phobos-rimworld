using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class ArrivalFuelRuleTests
{
    [Fact]
    public void TicksPerTileFollowVehicleFramework() => Assert.Equal(2040, ArrivalFuelRule.TicksPerTile(10.4), 6);    // round(5.77) = 6

    [Fact]
    public void AVehicleThatCannotMoveCrossesNothing() => Assert.Equal(0, ArrivalFuelRule.TicksPerTile(0));

    [Fact]
    public void AnM113BurnsAboutOneUnitATile() => Assert.Equal(1.02, ArrivalFuelRule.TripFuel(1, 20, 7), 2);    // 20/60000 x 3060

    [Fact]
    public void ANearRaidArrivesNearlyFull() => Assert.Equal(90, ArrivalFuelRule.Arrival(100, 10), 6);

    [Fact]
    public void AFarRaidStopsAtTheFloor() => Assert.Equal(20, ArrivalFuelRule.Arrival(100, 500), 6);

    [Fact]
    public void NoTripIsAFullTank() => Assert.Equal(100, ArrivalFuelRule.Arrival(100, 0), 6);

    [Fact]
    public void NoTankIsNoFuel() => Assert.Equal(0, ArrivalFuelRule.Arrival(0, 5));
}
