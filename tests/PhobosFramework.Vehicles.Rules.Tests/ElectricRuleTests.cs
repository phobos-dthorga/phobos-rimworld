using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// The traction battery's temperature (Systems_Electric.xml; docs/development/vehicle-electric-design.md), through the integrator:
/// the thermal management holds the pack near 25 degrees C while the vehicle is on or charging, and a parked, unplugged pack drifts
/// slowly to the air. These pin the authored figures, so a change to them is a deliberate one.
/// </summary>
public class ElectricRuleTests
{
    const double Managed = 25, ManagementRate = 2, InsulationRate = 0.3, DischargeHeat = 12, ChargeHeat = 3;

    static double Run(double start, double outside, bool managed, double load, bool charging, double hours)
    {
        var values = new[] { start };
        var flows = new List<FlowSample> { new() { Quantity = 0, Kind = FlowKind.Toward, Rate = InsulationRate, Target = outside } };
        if (managed) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Toward, Rate = ManagementRate, Target = Managed });
        if (load > 0) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Into, Rate = DischargeHeat * load });
        if (charging) flows.Add(new FlowSample { Quantity = 0, Kind = FlowKind.Into, Rate = ChargeHeat });
        SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 200.0 }, flows, hours);
        return values[0];
    }

    [Fact]
    public void DrivenHardInFrostThePackStaysComfortable()
    {
        double pack = Run(-20, -20, managed: true, load: 1, charging: false, hours: 6);
        Assert.InRange(pack, 15, 40); // 12 + 2(25 - T) + 0.3(-20 - T) = 0: about 24 degrees
    }

    [Fact]
    public void ParkedAndUnpluggedInFrostItTakesHoursToFreeze()
    {
        double afterTwoHours = Run(20, -20, managed: false, load: 0, charging: false, hours: 2);
        Assert.True(afterTwoHours > 0, "a heavy pack cools slowly: still above freezing after two hours");
        double afterADay = Run(20, -20, managed: false, load: 0, charging: false, hours: 24);
        Assert.True(afterADay < -10, "after a day parked in frost it is frozen (charging slows to a crawl until it warms)");
    }

    [Fact]
    public void PluggedInItWarmsTowardTheSetPoint()
    {
        double pack = Run(-20, -20, managed: true, load: 0, charging: true, hours: 3);
        Assert.True(pack > 5, "charging and the pack heater bring a frozen pack out of the cold stages within a few hours");
    }

    [Fact]
    public void InTheDesertSunNormalDrivingStaysBelowTheWarmStage()
    {
        double pack = Run(30, 50, managed: true, load: 0.7, charging: false, hours: 8);
        Assert.True(pack < 40, "the thermal management keeps an undamaged pack out of the warm stage even at 50 degrees outside");
    }
}
