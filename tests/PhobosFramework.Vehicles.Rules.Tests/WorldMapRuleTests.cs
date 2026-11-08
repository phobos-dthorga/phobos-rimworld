using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The world-map rules (docs/development/vehicle-world-map-design.md): fuel correction, caravan caps, and the cab's climate.</summary>
public class WorldMapRuleTests
{
    [Fact]
    public void TheFuelCorrectionIsZeroAtANominalCruiseAndFollowsVehicleFrameworksRate()
    {
        Assert.Equal(0, FuelRules.ExtraPerHour(10, 1, 1), 6);
        Assert.Equal(5, FuelRules.ExtraPerHour(10, 1, 1.5), 6);     // labouring: half as much again
        Assert.Equal(-2, FuelRules.ExtraPerHour(10, 0.5, 0.3), 6);  // idling easier than VF's half rate: given back
        // On the world map VF burns per tick while moving; the Mule halves its world rate, and so does the correction.
        Assert.Equal(2.5, FuelRules.WorldPerHour(0.001, 2500), 6);
        Assert.Equal(1.25, FuelRules.ExtraPerHour(FuelRules.WorldPerHour(0.001, 2500), 1, 1.5), 6);
    }

    [Fact]
    public void ACaravanMovesAsItsSlowestVehicle()
    {
        Assert.Equal(1, CaravanRules.CombineCaps(new List<double>()), 6);
        Assert.Equal(0.6, CaravanRules.CombineCaps(new[] { 1.0, 0.6, 0.9 }), 6);
        Assert.Equal(DrivetrainModel.CrawlFloor, CaravanRules.CombineCaps(new[] { 0.05 }), 6);
        Assert.Equal(100, CaravanRules.TicksPerMove(100, 1));
        Assert.Equal(200, CaravanRules.TicksPerMove(100, 0.5));
    }

    // The cab climate (Systems_Cabin.xml): a closed cab drifts toward the outside at insulationRate and toward the heater's or the
    // cooler's target while one runs; all through the integrator's exact exponential steps.
    const double InsulationRate = 2, HeaterRate = 6, HeaterTarget = 18, CoolerRate = 4, CoolerTarget = 24, NightHeaterRate = 3;

    static double Run(double start, double outside, bool heater, bool cooler, bool nightHeater, double hours)
    {
        var values = new[] { start };
        var flows = new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Toward, Rate = InsulationRate, Target = outside } };
        if (heater) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Toward, Rate = HeaterRate, Target = HeaterTarget });
        if (cooler) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Toward, Rate = CoolerRate, Target = CoolerTarget });
        if (nightHeater) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Toward, Rate = NightHeaterRate, Target = HeaterTarget });
        SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 80.0 }, flows, hours);
        return values[0];
    }

    [Fact]
    public void AHeatedCabInFrostSettlesWarm()
    {
        // At -20 outside with the heater on, the cab settles where 6 x (18 - T) balances 2 x (T + 20): 8.5 °C: cold, not freezing.
        Assert.InRange(Run(-20, -20, true, false, false, 1), 7, 10);
        // Mild outside, the heater holds it close to its target.
        Assert.InRange(Run(5, 5, true, false, false, 1), 14, 16);
    }

    [Fact]
    public void AnUnheatedCabFollowsTheOutside()
    {
        double afterAnHour = Run(18, -20, false, false, false, 1);
        Assert.InRange(afterAnHour, -16, -12);   // 2 an hour: 86% of the way to -20
        Assert.InRange(Run(18, -20, false, false, false, 6), -20.1, -19.5);
    }

    [Fact]
    public void AnAirConditionedCabInHeatStaysUnderThirty()
    {
        Assert.InRange(Run(40, 40, false, true, false, 1), 28, 30);
    }

    [Fact]
    public void TheNightHeaterKeepsACampedCabAboveFreezingInFrost()
    {
        Assert.True(Run(-10, -10, false, false, true, 2) > 5, "3 x (18 - T) balances 2 x (T + 10): about 6.8 °C");
    }
}
