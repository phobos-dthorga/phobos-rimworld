using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Aircraft service-life upgrades (docs/development/aircraft-service-life-upgrades.md): a re-engined turbine and the kits' capabilities.</summary>
public class ServiceLifeUpgradeTests
{
    [Fact]
    public void ABiggerEngineRunsCoolerForTheSameWork()
    {
        var stock = new TurbineSpec();
        var uprated = new TurbineSpec { OutputFactor = 1.15 };
        TurbineState before = TurbineModel.Evaluate(stock, 0.9, 15, 1, 2, 2, true);
        TurbineState after = TurbineModel.Evaluate(uprated, 0.9, 15, 1, 2, 2, true);
        Assert.Equal(before.Available * 1.15, after.Available, 9);
        Assert.True(after.Temperature < before.Temperature);
    }

    [Fact]
    public void OnOneEngineABiggerEngineHasMoreInReserve()
    {
        var stock = new TurbineSpec();
        var uprated = new TurbineSpec { OutputFactor = 1.15 };
        // One engine of two working: half the output; the bigger engine keeps more of it.
        Assert.Equal(0.5, TurbineModel.Evaluate(stock, 0.5, 15, 1, 1, 2, true).Available, 9);
        Assert.Equal(0.575, TurbineModel.Evaluate(uprated, 0.5, 15, 1, 1, 2, true).Available, 9);
    }

    [Fact]
    public void ABiggerEngineLiftsMore()
    {
        var stock = new TurbineSpec();
        var uprated = new TurbineSpec { OutputFactor = 1.1 };
        Assert.Equal(TurbineModel.LiftPowerAvailable(stock, 15, 1, 1) * 1.1, TurbineModel.LiftPowerAvailable(uprated, 15, 1, 1), 9);
    }

    [Fact]
    public void AnEngineThatKeepsMoreInHotAirLosesLessOnAHotDay()
    {
        var stock = new TurbineSpec();
        var hotter = new TurbineSpec { DeratePerDegree = stock.DeratePerDegree * 0.5 };
        Assert.True(TurbineModel.Derate(hotter, 35) > TurbineModel.Derate(stock, 35));
        Assert.Equal(TurbineModel.Derate(stock, 15), TurbineModel.Derate(hotter, 15), 9);
    }

    [Fact]
    public void AsDesignedNothingChanges()
    {
        var spec = new TurbineSpec();
        Assert.Equal(1, spec.OutputFactor);
        Assert.Equal(1, TurbineModel.Evaluate(spec, 0.7, 15, 1, 2, 2, true).Available, 9);
    }

    [Fact]
    public void TheKitsCapabilitiesAreFigures()
    {
        var early = new AvionicsFitValues { Sight = AvionicsLevel.Basic };
        var values = AvionicsRule.Parameters(early).ToDictionary(p => p.name, p => p.value);
        Assert.Equal(0, values["autopilot"]);
        Assert.Equal(1, values["sight"]);
        Assert.Equal(0, values["sightFull"]);
        Assert.Equal(0, values["nightSight"]);
        Assert.Equal(0, values["radar"]);

        var late = new AvionicsFitValues { Autopilot = true, Sight = AvionicsLevel.Full, NightSight = true, Radar = true };
        values = AvionicsRule.Parameters(late).ToDictionary(p => p.name, p => p.value);
        Assert.Equal(1, values["autopilot"]);
        Assert.Equal(1, values["sightFull"]);
        Assert.Equal(1, values["nightSight"]);
        Assert.Equal(1, values["radar"]);
    }
}
