using System;
using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class DriveHeatRuleTests
{
    const double Stopped = 0.1, Cruising = 0.7, FlatOut = 1, IdleOil = 55, FullOil = 95;

    static double Gearbox(bool running, bool moving, bool modelled, double load) =>
        DriveHeatRules.GearboxWork(running, moving, modelled, load, Stopped, Cruising, FlatOut);

    [Fact]
    public void AGearboxMakesNoHeatWithTheEngineOff() => Assert.Equal(0, Gearbox(false, true, true, 1));

    [Fact]
    public void StandingWithTheEngineRunningWarmsItALittle() => Assert.Equal(Stopped, Gearbox(true, false, true, 0.9));

    [Fact]
    public void WithoutTheModelMovingIsFullWork() => Assert.Equal(FlatOut, Gearbox(true, true, false, 0.2));

    [Fact]
    public void ALightCruiseStillWarmsItAndFullLoadWarmsItMost()
    {
        // The owner's Abrams (8 Nov 2026): a tenth of rated power on the move left the gearbox at the air's temperature.
        Assert.True(Gearbox(true, true, true, 0.1) > 0.7);
        Assert.Equal(FlatOut, Gearbox(true, true, true, 1), 6);
        Assert.Equal(FlatOut, Gearbox(true, true, true, 2), 6);
    }

    [Fact]
    public void TheShippedGearboxSettlesWarmOnACruise()
    {
        // Systems_Chassis.xml: 180 degrees an hour at full work, cooling at 2 + 0.5 of the gap an hour on the move.
        double rise = 180 * Gearbox(true, true, true, 0.15) / 2.5;
        Assert.InRange(rise, 50, 72);
    }

    [Fact]
    public void OilWithoutCoolantIsAtTheAirWhileOff() =>
        Assert.Equal(-15, DriveHeatRules.OilTargetWithoutCoolant(false, -15, 1, IdleOil, FullOil));

    [Fact]
    public void OilWithoutCoolantRunsWarmWhateverTheAir()
    {
        Assert.Equal(IdleOil, DriveHeatRules.OilTargetWithoutCoolant(true, -15, 0, IdleOil, FullOil), 6);
        Assert.Equal(FullOil, DriveHeatRules.OilTargetWithoutCoolant(true, -15, 1, IdleOil, FullOil), 6);
        Assert.Equal(75, DriveHeatRules.OilTargetWithoutCoolant(true, -15, 0.5, IdleOil, FullOil), 6);
    }

    [Fact]
    public void OilIsNeverCoolerThanTheAir() =>
        Assert.Equal(70, DriveHeatRules.OilTargetWithoutCoolant(true, 70, 0, IdleOil, FullOil));

    static double After(double start, double target, double rate, double hours)
    {
        double[] values = { start };
        SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 1500.0 },
            new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Toward, Rate = rate, Target = target } }, hours);
        return values[0];
    }

    [Fact]
    public void TheShippedTurbineReachesItsTemperatureWithinOneUpdate()
    {
        // Systems_Turbine.xml: follows the model at 60 of the gap an hour while running; the systems update every 0.1 game hours.
        Assert.InRange(After(-5, 700, 60, 0.1), 697, 700);
    }

    [Fact]
    public void TheShippedTurbineCoolsOverAboutAnHourAfterShutdown()
    {
        // Systems_Turbine.xml: 2.5 of the gap an hour once it is off, so it is still hot a quarter hour later and near the air in two.
        Assert.InRange(After(700, -5, 2.5, 0.25), 350, 400);
        Assert.InRange(After(700, -5, 2.5, 2), -5, 60);
    }
}
